using System.Net;
using System.Net.Sockets;

const int GatewayPort = 8080;
 
Dictionary<string, (string Host, int Port)> Routes = new()
{
	{ "/api/menu", ("127.0.0.1", 9001) },
	{ "/api/orders", ("127.0.0.1", 9002) },
	{ "/api/reports", ("127.0.0.1", 9003) }
};


var listener = new TcpListener(IPAddress.Any, GatewayPort);
listener.Start();
Console.WriteLine($"API Gateway запущен на порту {GatewayPort}...");
Console.WriteLine("Правила маршрутизации:");
foreach (var route in Routes)
{
	Console.WriteLine($"  {route.Key} -> {route.Value.Host}:{route.Value.Port}");
}

while (true)
{
	var client = listener.AcceptTcpClient();
	_ = Task.Run(() => HandleClient(client));
}

async Task HandleClient(TcpClient client)
{
	Console.WriteLine($"[Gateway] Новое подключение от {client.Client.RemoteEndPoint}");
	await using var clientStream = client.GetStream();

	using var reader = new StreamReader(clientStream, leaveOpen: true);

	var requestLine = await reader.ReadLineAsync();
	if (string.IsNullOrEmpty(requestLine))
	{
		Console.WriteLine("[Gateway] Пустой запрос, закрываем соединение.");
		client.Close();
		return;
	}

	var parts = requestLine.Split(' ');
	var method = parts[0];
	var fullPath = parts[1];

	string targetPrefix = null;
	(string targetHost, int targetPort) = (null, 0);

	foreach (var route in Routes)
	{
		if (fullPath.StartsWith(route.Key))
		{
			targetPrefix = route.Key;
			(targetHost, targetPort) = route.Value;
			break;
		}
	}

	if (targetHost == null)
	{
		await SendErrorResponse(clientStream, "502 Bad Gateway", "Не удалось найти сервис для данного маршрута.");
		client.Close();
		return;
	}

	TcpClient serviceClient = null;
	try
	{
		serviceClient = new TcpClient();
		await serviceClient.ConnectAsync(targetHost, targetPort);
		await using var serviceStream = serviceClient.GetStream();

		var newPath = fullPath.Substring(targetPrefix.Length);
		if (string.IsNullOrEmpty(newPath)) newPath = "/";

		var newRequestLine = $"{method} {newPath}\r\n";

		var requestLineBytes = System.Text.Encoding.UTF8.GetBytes(newRequestLine);
		await serviceStream.WriteAsync(requestLineBytes, 0, requestLineBytes.Length);

		await clientStream.CopyToAsync(serviceStream);

		await serviceStream.CopyToAsync(clientStream);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"[Gateway] Ошибка проксирования: {ex.Message}");
		await SendErrorResponse(clientStream, "503 Service Unavailable", "Целевой сервис недоступен или произошла ошибка.");
	}
	finally
	{
		client.Close();
		serviceClient?.Close();
		Console.WriteLine($"[Gateway] Соединение с {client.Client.RemoteEndPoint} закрыто.");
	}
}

async Task SendErrorResponse(Stream stream, string status, string message)
{
	try
	{
		await using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };
		var response = $"{status}\r\nContent-Type: application/json\r\n\r\n{{\"error\":\"{message}\"}}";
		await writer.WriteAsync(response);
	}
	catch
	{
		
	}
}
