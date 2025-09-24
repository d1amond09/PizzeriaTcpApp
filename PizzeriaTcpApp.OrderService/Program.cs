using System.Net;
using System.Net.Sockets;
using PizzeriaTcpApp.OrderService.Presentation;

const int Port = 9002;
const string MenuServiceHost = "127.0.0.1";
const int MenuServicePort = 9001;

var listener = new TcpListener(IPAddress.Any, Port);
listener.Start();
Console.WriteLine($"OrderService запущен на порту {Port}...");

while (true)
{
	var client = listener.AcceptTcpClient();
	Task.Run(() => ServerHelper.HandleClient(client, MenuServiceHost, MenuServicePort));
}


