namespace PizzeriaTcpApp.Shared.Contracts;

public record OrderItemDto(Guid Id, Guid OrderId, Guid PizzaId, string PizzaName, int Quantity, decimal PriceAtTimeOfOrder);