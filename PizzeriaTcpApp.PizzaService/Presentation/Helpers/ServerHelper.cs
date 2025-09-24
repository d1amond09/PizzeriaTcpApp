using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using PizzeriaTcpApp.PizzaService.Application.Services.Interfaces;
using PizzeriaTcpApp.PizzaService.Application.Services.Implementations;
using PizzeriaTcpApp.PizzaService.Domain.Models;

namespace PizzeriaTcpApp.PizzaService.Presentation.Helpers;

public class ServerHelper
{
	private static readonly IPizzasService _pizzaService = new PizzasService();

	public static void HandleClient(TcpClient client)
	{
		Console.WriteLine("New connection!");
		using var stream = client.GetStream();
		using var reader = new StreamReader(stream, Encoding.UTF8);
		using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
		try
		{
			// 1. Читаем стартовую строку
			var requestLine = reader.ReadLine();
			if (string.IsNullOrEmpty(requestLine)) return;
			Console.WriteLine(requestLine);

			var parts = requestLine.Split(' ');
			var method = parts[0];
			var path = parts[1];

			// 2. Читаем все заголовки
			var headers = new Dictionary<string, string>();
			string line;
			while (!string.IsNullOrEmpty(line = reader.ReadLine()))
			{
				var headerParts = line.Split(':', 2);
				if (headerParts.Length == 2)
				{
					headers[headerParts[0].Trim()] = headerParts[1].Trim();
				}
			}

			// 3. Читаем тело, если есть Content-Length
			string body = null;
			if (headers.TryGetValue("Content-Length", out var contentLengthValue))
			{
				if (int.TryParse(contentLengthValue, out var contentLength))
				{
					var buffer = new char[contentLength];
					reader.ReadBlock(buffer, 0, contentLength);
					body = new string(buffer);
				}
			}

			string response;


			if (method == "GET" && path == "/pizzas")
			{
				var allPizzas = _pizzaService.GetAllPizzas();
				response = CreateResponse("200 OK", allPizzas);
			}
			else if (method == "GET" && path.StartsWith("/pizzas/"))
			{
				var id = Guid.Parse(path.Split('/')[2]);
				var pizza = _pizzaService.GetPizzaById(id);
				response = pizza != null ? CreateResponse("200 OK", pizza) : CreateResponse("404 Not Found");
			}
			else if (method == "POST" && path == "/pizzas")
			{
				var pizzaDto = JsonSerializer.Deserialize<Pizza>(body); 
				var createdPizza = _pizzaService.CreatePizza(pizzaDto);
				Console.WriteLine($"Добавлена пицца: {createdPizza.Name}");
				response = CreateResponse("201 Created", createdPizza);
			}
			else if (method == "PUT" && path.StartsWith("/pizzas/"))
			{
				var id = Guid.Parse(path.Split('/')[2]);
				var pizzaDto = JsonSerializer.Deserialize<Pizza>(body);
				var updatedPizza = _pizzaService.UpdatePizza(id, pizzaDto);
				if (updatedPizza != null)
				{
					Console.WriteLine($"Обновлена пицца ID: {id}");
					response = CreateResponse("200 OK", updatedPizza);
				}
				else
				{
					response = CreateResponse("404 Not Found");
				}
			}
			else if (method == "DELETE" && path.StartsWith("/pizzas/"))
			{
				var id = Guid.Parse(path.Split('/')[2]);
				var success = _pizzaService.DeletePizza(id);
				if (success)
				{
					Console.WriteLine($"Удалена пицца ID: {id}");
					response = CreateResponse("204 No Content");
				}
				else
				{
					response = CreateResponse("404 Not Found");
				}
			}
			else
			{
				response = CreateResponse("400 Bad Request");
			}

			writer.Write(response);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Ошибка: {ex.Message}");
			var errorResponse = CreateResponse("500 Internal Server Error");
			writer.Write(errorResponse);
		}
		finally
		{
			client.Close();
			Console.WriteLine("Соединение закрыто.");
		}
	}

	public static string CreateResponse<T>(string status, T body)
	{
		var jsonOptions = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles };
		var jsonBody = JsonSerializer.Serialize(body);
		return $"{status}\r\nContent-Type: application/json\r\n\r\n{jsonBody}";
	}

	public static string CreateResponse(string status)
	{
		return $"{status}\r\n\r\n";
	}
}
