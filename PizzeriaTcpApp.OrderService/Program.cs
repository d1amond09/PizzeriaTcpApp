using System.Net;
using System.Net.Sockets;
using PizzeriaTcpApp.OrderService.Presentation;

const int Port = 9002;
const string ApiGatewayHost = "127.0.0.1";
const int ApiGatewayPort = 8080;

var listener = new TcpListener(IPAddress.Any, Port);
listener.Start();
Console.WriteLine($"OrderService запущен на порту {Port}...");

while (true)
{
	var client = listener.AcceptTcpClient();
	Task.Run(() => ServerHelper.HandleClient(client, ApiGatewayHost, ApiGatewayPort));
}


