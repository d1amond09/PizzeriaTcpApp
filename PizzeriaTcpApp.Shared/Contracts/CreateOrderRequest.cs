namespace PizzeriaTcpApp.Shared.Contracts;

public class CreateOrderRequest
{
	public string CustomerName { get; set; } = string.Empty;
	public List<OrderItemDtoLess> Items { get; set; } = [];
}

public class OrderItemDtoLess
{
	public Guid PizzaId { get; set; }
	public int Quantity { get; set; }
}
