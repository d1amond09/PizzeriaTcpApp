using System.Net;
using System.Net.Sockets;
using System.Text;

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
	_ = Task.Run(() => HandleProxyConnection(client));
}

async Task HandleProxyConnection(TcpClient client)
{
	var clientEndPoint = client?.Client?.RemoteEndPoint?.ToString() ?? "unknown client";
	Console.WriteLine($"[Gateway] New connection from {clientEndPoint}");

	await using var clientStream = client.GetStream();

	using var initialData = new MemoryStream();
	var buffer = new byte[8192];

	var bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length);
	if (bytesRead == 0)
	{
		client.Close();
		return;
	}
	initialData.Write(buffer, 0, bytesRead);
	initialData.Position = 0;

	string requestLine;
	using (var reader = new StreamReader(initialData, Encoding.UTF8, leaveOpen: true))
	{
		requestLine = await reader.ReadLineAsync();
	}
	initialData.Position = 0; 

	if (string.IsNullOrEmpty(requestLine))
	{
		client.Close();
		return;
	}

	var parts = requestLine.Split(' ');
	var fullPath = parts.Length > 1 ? parts[1] : "/";

	var (routePrefix, target) = FindRoute(fullPath);
	if (target == null)
	{
		await SendErrorResponse(clientStream, "502 Bad Gateway", "Route not found for the given path.");
		client.Close();
		return;
	}

	TcpClient serviceClient = null;
	try
	{
		serviceClient = new TcpClient();
		await serviceClient.ConnectAsync(target.Value.Host, target.Value.Port);
		await using var serviceStream = serviceClient.GetStream();

		Console.WriteLine($"[Gateway] Proxying {fullPath} to {target.Value.Host}:{target.Value.Port}");

		await initialData.CopyToAsync(serviceStream);

		var clientToServer = clientStream.CopyToAsync(serviceStream);
		var serverToClient = serviceStream.CopyToAsync(clientStream);

		await Task.WhenAll(clientToServer, serverToClient);
	}
	catch (Exception ex)
	{
		Console.WriteLine($"[Gateway] Proxy error: {ex.GetType().Name} - {ex.Message}");
	}
	finally
	{
		client?.Close();
		serviceClient?.Close();
		Console.WriteLine($"[Gateway] Connection from {clientEndPoint} closed.");
	}
}

(string, (string Host, int Port)?) FindRoute(string path)
{
	var bestMatch = Routes.Keys
		.Where(path.StartsWith)
		.OrderByDescending(k => k.Length)
		.FirstOrDefault();

	return bestMatch != null ? (bestMatch, Routes[bestMatch]) : (null, null);
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
