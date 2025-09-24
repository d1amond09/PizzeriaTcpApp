namespace PizzeriaTcpApp.Wpf.Models;

public class Pizza
{
	public Guid Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public List<string> Ingredients { get; set; } = [];
	public decimal Price { get; set; }
}
