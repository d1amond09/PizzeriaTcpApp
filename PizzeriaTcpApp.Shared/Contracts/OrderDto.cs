namespace PizzeriaTcpApp.Shared.Contracts;

public record OrderDto(Guid Id, string CustomerName, DateTime OrderDate, decimal TotalPrice, string Status, List<OrderItemDto> Items);