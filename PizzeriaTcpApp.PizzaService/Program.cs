using System.Net;
using System.Net.Sockets;
using PizzeriaTcpApp.PizzaService.Presentation.Helpers;

var listener = new TcpListener(IPAddress.Any, 8888);
listener.Start();
Console.WriteLine("Server started on port: 8888...");
Console.WriteLine("Используется PizzaService для управления данными.");

while (true)
{
	var client = listener.AcceptTcpClient();
	await Task.Run(() => ServerHelper.HandleClient(client));
}

