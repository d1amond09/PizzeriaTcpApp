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

	public static async Task HandleClient(TcpClient client)
	{
		Console.WriteLine("Новое подключение к MenuService!");
		await using var stream = client.GetStream();
		using var reader = new StreamReader(stream, leaveOpen: true);
		await using var writer = new StreamWriter(stream) { AutoFlush = true };

		try
		{
			var requestLine = await reader.ReadLineAsync();
			if (string.IsNullOrEmpty(requestLine)) return;

			Console.WriteLine(requestLine);

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
				if (int.TryParse(contentLengthValue, out var contentLength) && contentLength > 0)
				{
					var buffer = new char[contentLength];
					await reader.ReadBlockAsync(buffer, 0, contentLength);
					body = new string(buffer);
				}
			}

			string response;
			string api = "/api/menu";
			if (method == "GET" && path == $"{api}/pizzas")
			{
				var allPizzas = _pizzaService.GetAllPizzas();
				Console.WriteLine(string.Join(',', allPizzas.Select(x => x.Name)));
				response = CreateResponse("200 OK", allPizzas);
			}
			else if (method == "GET" && path.StartsWith($"{api}/pizzas/"))
			{
				var idStr = path.Substring("/api/menu/pizzas/".Length);
				if (Guid.TryParse(idStr, out Guid id))
				{
					var pizza = _pizzaService.GetPizzaById(id);
					response = pizza != null
						? CreateResponse("200 OK", pizza)
						: CreateResponse("404 Not Found", new { error = $"Pizza with id={id} not found." });
				}
				else
				{
					response = CreateResponse("400 Bad Request", new { error = "Invalid pizza id format." });
				}
			}
			else if (method == "POST" && path == $"{api}/pizzas")
			{
				if (body == null)
				{
					response = CreateResponse("400 Bad Request", new { error = "Request body is missing for POST." });
				}
				else
				{
					try
					{
						var newPizza = JsonSerializer.Deserialize<Pizza>(body);
						var createdPizza = _pizzaService.CreatePizza(newPizza);
						Console.WriteLine($"Добавлена пицца: {createdPizza.Name}");
						response = CreateResponse("201 Created", createdPizza);
					}
					catch (JsonException ex)
					{
						response = CreateResponse("400 Bad Request", new { error = $"Invalid JSON format: {ex.Message}" });
					}
				}
			}
			else if (method == "PUT" && path.StartsWith("/api/menu/pizzas/"))
			{
				var idStr = path.Substring("/api/menu/pizzas/".Length);
				if (Guid.TryParse(idStr, out Guid id) && body != null)
				{
					var updatedPizzaData = JsonSerializer.Deserialize<Pizza>(body);
					var result = _pizzaService.UpdatePizza(id, updatedPizzaData);
					response = result != null
						? CreateResponse("200 OK", result)
						: CreateResponse("404 Not Found", new { error = "Pizza not found for update." });
				}
				else
				{
					response = CreateResponse("400 Bad Request", new { error = "Invalid ID or missing body." });
				}
			}
			else if (method == "DELETE" && path.StartsWith("/api/menu/pizzas/"))
			{
				var idStr = path.Substring("/api/menu/pizzas/".Length);
				if (Guid.TryParse(idStr, out Guid id))
				{
					var success = _pizzaService.DeletePizza(id);
					response = success
						? CreateResponse("204 No Content", "") 
						: CreateResponse("404 Not Found", new { error = "Pizza not found for deletion." });
				}
				else
				{
					response = CreateResponse("400 Bad Request", new { error = "Invalid ID format." });
				}
			}
			else
			{
				response = CreateResponse("404 Not Found", new { error = "Endpoint not found in MenuService." });
			}

			await writer.WriteAsync(response);
			Console.WriteLine(response);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[MenuService] Критическая ошибка: {ex.Message}");
		}
		finally
		{
			client.Close();
			Console.WriteLine("Соединение с MenuService закрыто.");
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
