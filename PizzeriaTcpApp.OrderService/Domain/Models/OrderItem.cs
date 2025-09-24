namespace PizzeriaTcpApp.OrderService.Domain.Models;

public class OrderItem
{
	public Guid Id { get; set; }
	public Guid OrderId { get; set; }
	public Guid PizzaId { get; set; }
	public string PizzaName { get; set; } 
	public int Quantity { get; set; }
	public decimal PriceAtTimeOfOrder { get; set; }
}
