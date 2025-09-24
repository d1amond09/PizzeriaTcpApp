using Microsoft.EntityFrameworkCore;
using PizzeriaTcpApp.PizzaService.Application.Services.Interfaces;
using PizzeriaTcpApp.PizzaService.Domain.Models;
using PizzeriaTcpApp.PizzaService.Infrastructure.Persistence.Common;

namespace PizzeriaTcpApp.PizzaService.Application.Services.Implementations;

public class PizzasService : IPizzasService
{
	private readonly List<Pizza> _pizzas = [];

	public PizzasService()
	{
		using var context = new AppDbContext();
		context.Database.Migrate();

		if (!context.Pizzas.Any())
		{
			context.Pizzas.AddRange(
				new Pizza { Name = "Маргарита", Ingredients = ["Сыр Моцарелла", "Томаты", "Томатный соус", "Базилик"], Price = 450 },
				new Pizza { Name = "Четыре сыра", Ingredients = ["Моцарелла", "Дорблю", "Пармезан", "Чеддер"], Price = 600 },
				new Pizza { Name = "Пепперони", Ingredients = ["Сыр Моцарелла", "Салями Пепперони", "Томатный соус"], Price = 550 }
			);
			context.SaveChanges();
		}
	}

	public Pizza? CreatePizza(Pizza? newPizza)
	{
		ArgumentNullException.ThrowIfNull(newPizza);
		newPizza.Id = Guid.NewGuid();
		using var context = new AppDbContext();
		context.Pizzas.Add(newPizza);
		context.SaveChanges(); 
		return newPizza;
	}

	public bool DeletePizza(Guid id)
	{
		using var context = new AppDbContext();
		var pizzaToRemove = context.Pizzas.FirstOrDefault(p => p.Id == id);
		if (pizzaToRemove == null)
		{
			return false;
		}
		context.Pizzas.Remove(pizzaToRemove);
		context.SaveChanges();
		return true;
	}

	public IEnumerable<Pizza> GetAllPizzas()
	{
		using var context = new AppDbContext();
		return [.. context.Pizzas.AsNoTracking()];
	}

	public Pizza? GetPizzaById(Guid id)
	{
		using var context = new AppDbContext();
		return context.Pizzas.AsNoTracking().FirstOrDefault(p => p.Id == id);
	}

	public Pizza? UpdatePizza(Guid id, Pizza? updatedPizza)
	{
		using var context = new AppDbContext();
		var existingPizza = context.Pizzas.FirstOrDefault(p => p.Id == id);
		if (existingPizza == null)
		{
			return null;
		}

		existingPizza.Name = updatedPizza?.Name ?? "";
		existingPizza.Ingredients = updatedPizza?.Ingredients ?? [];
		existingPizza.Price = updatedPizza?.Price ?? 0;

		context.SaveChanges();
		return existingPizza;
	}
}
