namespace PizzeriaTcpApp.Shared.Contracts;

public class OrderDto
{
	public Guid Id { get; set; }
	public string CustomerName { get; set; } = string.Empty;
	public DateTime OrderDate { get; set; }
	public decimal TotalPrice { get; set; }
	public string Status { get; set; } 
	public List<OrderItemDto> Items { get; set; } = [];
}
