using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using PizzeriaTcpApp.OrderService.Application.Services.Implementations;
using PizzeriaTcpApp.OrderService.Application.Services.Interfaces;
using PizzeriaTcpApp.OrderService.Domain.Models;
using PizzeriaTcpApp.Shared.Contracts;
using TcpRestNetworking;
using TcpRestNetworking.Exceptions;

namespace PizzeriaTcpApp.OrderService.Presentation;

public static class ServerHelper
{
	private static readonly IOrderDbService _orderDbService = new OrderDbService();

	public static async void HandleClient(TcpClient client, string menuServiceHost, int menuServicePort)
	{
		Console.WriteLine("Новое подключение к OrderService!");
		using (var stream = client.GetStream())
		using (var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true))
		using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
		{
			try
			{
				var requestLine = await reader.ReadLineAsync();
				if (string.IsNullOrEmpty(requestLine)) return;

				var parts = requestLine.Split(' ');
				var method = parts[0];
				var path = parts[1];

				var headers = new Dictionary<string, string>();
				string line;
				while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
				{
					var headerParts = line.Split(':', 2);
					if (headerParts.Length == 2)
					{
						headers[headerParts[0].Trim()] = headerParts[1].Trim();
					}
				}

				string body = null;
				if (headers.TryGetValue("Content-Length", out var contentLengthValue))
				{
					if (int.TryParse(contentLengthValue, out var contentLength))
					{
						var buffer = new char[contentLength];
						await reader.ReadBlockAsync(buffer, 0, contentLength);
						body = new string(buffer);
					}
				}

				string response;

				if (method == "GET" && path == "/orders")
				{
					var orders = _orderDbService.GetAllOrders();
					response = CreateResponse("200 OK", orders);
				}
				else if (method == "POST" && path == "/orders")
				{
					response = await HandleCreateOrder(body, menuServiceHost, menuServicePort);
				}
				else
				{
					response = CreateResponse("404 Not Found", new { error = "Endpoint not found in OrderService" });
				}

				await writer.WriteAsync(response);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[OrderService] Ошибка: {ex.Message}");
				var errorResponse = CreateResponse("500 Internal Server Error", new { error = ex.Message });
				await writer.WriteAsync(errorResponse);
			}
			finally
			{
				client.Close();
				Console.WriteLine("Соединение с OrderService закрыто.");
			}
		}
	}

	private static async Task<string> HandleCreateOrder(string body, string menuServiceHost, int menuServicePort)
	{
		try
		{
			var requestDto = JsonSerializer.Deserialize<CreateOrderRequest>(body);
			var menuClient = new RestOverTcpClient(menuServiceHost, menuServicePort);

			var newOrder = new Order
			{
				CustomerName = requestDto.CustomerName,
				OrderDate = DateTime.UtcNow,
				Status = "Принят" // Начальный статус
			};

			foreach (var itemDto in requestDto.Items)
			{
				PizzaDto pizza;
				try
				{
					pizza = await menuClient.GetAsync<PizzaDto>($"/pizzas/{itemDto.PizzaId}");
				}
				catch (ApiException ex)
				{
					Console.WriteLine($"Не удалось получить пиццу ID={itemDto.PizzaId} от MenuService: {ex.Message}");
					return CreateResponse("400 Bad Request", new { error = $"Не удалось найти пиццу с ID={itemDto.PizzaId}." });
				}

				var orderItem = new OrderItem
				{
					PizzaId = pizza.Id,
					PizzaName = pizza.Name,
					Quantity = itemDto.Quantity,
					PriceAtTimeOfOrder = pizza.Price 
				};
				newOrder.Items.Add(orderItem);
				newOrder.TotalPrice += orderItem.PriceAtTimeOfOrder * orderItem.Quantity;
			}

			var createdOrder = _orderDbService.CreateOrder(newOrder);
			Console.WriteLine($"Создан новый заказ ID={createdOrder.Id} на сумму {createdOrder.TotalPrice}");
			return CreateResponse("201 Created", createdOrder);
		}
		catch (JsonException)
		{
			return CreateResponse("400 Bad Request", new { error = "Invalid JSON format in request body" });
		}
	}

	private static string CreateResponse<T>(string status, T body)
	{
		var jsonOptions = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles };
		var jsonBody = JsonSerializer.Serialize(body, jsonOptions);
		return $"{status}\r\nContent-Type: application/json\r\n\r\n{jsonBody}";
	}

}
