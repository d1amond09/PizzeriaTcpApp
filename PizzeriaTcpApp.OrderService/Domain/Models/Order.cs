namespace PizzeriaTcpApp.OrderService.Domain.Models;

public class Order
{
	public Guid Id { get; set; }
	public string CustomerName { get; set; } = string.Empty;
	public DateTime OrderDate { get; set; }
	public decimal TotalPrice { get; set; }
	public string Status { get; set; } 
	public List<OrderItem> Items { get; set; } = [];
}
