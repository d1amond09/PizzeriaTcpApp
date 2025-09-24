using System.Net;
using System.Net.Sockets;
using System.Text.Encodings.Web;
using System.Text.Json;
using PizzeriaTcpApp.Shared.Contracts;
using TcpRestNetworking;

const int Port = 9003;
const string OrderServiceHost = "127.0.0.1";
const int OrderServicePort = 9002;

var listener = new TcpListener(IPAddress.Any, Port);
listener.Start();
Console.WriteLine($"ReportingService запущен на порту {Port}...");

while (true)
{
	var client = listener.AcceptTcpClient();
	_ = Task.Run(() => HandleClient(client));
}

async Task HandleClient(TcpClient client)
{
	Console.WriteLine("Новое подключение к ReportingService!");
	await using var stream = client.GetStream();
	using var reader = new StreamReader(stream, leaveOpen: true);
	await using var writer = new StreamWriter(stream) { AutoFlush = true };

	try
	{
		var requestLine = await reader.ReadLineAsync();
		if (string.IsNullOrEmpty(requestLine)) return;

		var parts = requestLine.Split(' ');
		var method = parts[0];
		var pathWithQuery = parts[1]; 

		while (!string.IsNullOrEmpty(await reader.ReadLineAsync())) ;

		string response;

		if (method == "GET")
		{
			var uri = new Uri("http://localhost" + pathWithQuery);
			var path = uri.AbsolutePath;

			response = path switch
			{
				"/api/reports/total-revenue" => await HandleTotalRevenue(uri),
				"/api/reports/most-popular-pizza" => await HandleMostPopularPizza(),
				"/api/reports/most-profitable-pizza" => await HandleMostProfitablePizza(),
				"/api/reports/average-order-value" => await HandleAverageOrderValue(),
				"/api/reports/orders-by-status" => await HandleOrdersByStatus(),
				_ => CreateResponse("404 Not Found", new { error = "Report not found" }),
			};
		}
		else
		{
			response = CreateResponse("405 Method Not Allowed", new { error = "Only GET method is supported" });
		}

		await writer.WriteAsync(response);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"[ReportingService] Ошибка: {ex.Message}");
		var errorResponse = CreateResponse("500 Internal Server Error", new { error = ex.Message });
		await writer.WriteAsync(errorResponse);
	}
	finally
	{
		client.Close();
		Console.WriteLine("Соединение с ReportingService закрыто.");
	}
}

#region Report Handlers

async Task<string> HandleTotalRevenue(Uri uri)
{
	var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
	if (!DateTime.TryParse(query["from"], out var fromDate) || !DateTime.TryParse(query["to"], out var toDate))
	{
		return CreateResponse("400 Bad Request", new { error = "Please provide 'from' and 'to' dates in a valid format." });
	}

	var orders = await GetAllOrdersFromService();
	var totalRevenue = orders
		.Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
		.Sum(o => o.TotalPrice);

	return CreateResponse("200 OK", new { fromDate, toDate, totalRevenue });
}
async Task<string> HandleMostPopularPizza()
{
	var orders = await GetAllOrdersFromService();
	if (!orders.Any()) return CreateResponse("200 OK", "");

	var result = orders
		.SelectMany(o => o.Items) 
		.GroupBy(item => new { item.PizzaId, item.PizzaName }) 
		.Select(g => new { g.Key.PizzaName, TotalQuantity = g.Sum(item => item.Quantity) }) 
		.OrderByDescending(x => x.TotalQuantity)
		.FirstOrDefault();

	return CreateResponse("200 OK", result);
}

async Task<string> HandleMostProfitablePizza()
{
	var orders = await GetAllOrdersFromService();
	if (!orders.Any()) return CreateResponse("200 OK", "");

	var result = orders
		.SelectMany(o => o.Items)
		.GroupBy(item => new { item.PizzaId, item.PizzaName })
		.Select(g => new { g.Key.PizzaName, TotalRevenue = g.Sum(item => item.Quantity * item.PriceAtTimeOfOrder) })
		.OrderByDescending(x => x.TotalRevenue)
		.FirstOrDefault();

	return CreateResponse("200 OK", result);
}

async Task<string> HandleAverageOrderValue()
{
	var orders = await GetAllOrdersFromService();
	if (!orders.Any()) return CreateResponse("200 OK", new { averageValue = 0, orderCount = 0 });

	var totalRevenue = orders.Sum(o => o.TotalPrice);
	var orderCount = orders.Count();
	var averageValue = totalRevenue / orderCount;

	return CreateResponse("200 OK", new { averageValue, orderCount, totalRevenue });
}

async Task<string> HandleOrdersByStatus()
{
	var orders = await GetAllOrdersFromService();
	var result = orders
		.GroupBy(o => o.Status)
		.Select(g => new { Status = g.Key, Count = g.Count() })
		.ToList();

	return CreateResponse("200 OK", result);
}

#endregion

#region Helper Methods

async Task<List<OrderDto>> GetAllOrdersFromService()
{
	try
	{
		var orderClient = new RestOverTcpClient(OrderServiceHost, OrderServicePort);
		var orders = await orderClient.GetAsync<List<OrderDto>>("/api/orders");
		return orders ?? new List<OrderDto>();
	}
	catch (Exception ex)
	{
		Console.WriteLine($"[ReportingService] Не удалось получить данные от OrderService: {ex.Message}");
		throw new InvalidOperationException("OrderService is unavailable.", ex);
	}
}

string CreateResponse<T>(string status, T? body)
{
	var jsonOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
	var jsonBody = JsonSerializer.Serialize(body, jsonOptions);
	return $"{status}\r\nContent-Type: application/json\r\n\r\n{jsonBody}";
}

#endregion