using System.Net;
using System.Net.Sockets;
using PizzeriaTcpApp.PizzaService.Presentation.Helpers;

var listener = new TcpListener(IPAddress.Any, 9001);
listener.Start();
Console.WriteLine("Server started on port: 9001...");
Console.WriteLine("Используется PizzaService для управления данными.");

while (true)
{
	var client = listener.AcceptTcpClient();
	await ServerHelper.HandleClient(client);
}

