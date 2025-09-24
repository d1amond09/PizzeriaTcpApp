namespace PizzeriaTcpApp.Shared.Contracts;

public record PizzaDto(Guid Id, string Name, List<string> Ingredients, decimal Price);