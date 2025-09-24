using PizzeriaTcpApp.PizzaService.Domain.Models;

namespace PizzeriaTcpApp.PizzaService.Application.Services.Interfaces;

public interface IPizzasService
{
	IEnumerable<Pizza> GetAllPizzas();

	Pizza? GetPizzaById(Guid id);

	Pizza? CreatePizza(Pizza? newPizza);

	Pizza? UpdatePizza(Guid id, Pizza? updatedPizza);

	bool DeletePizza(Guid id);
}
