using PizzeriaTcpApp.OrderService.Domain.Models;

namespace PizzeriaTcpApp.OrderService.Application.Services.Interfaces;

internal interface IOrderDbService
{
	Order CreateOrder(Order newOrder);
	IEnumerable<Order> GetAllOrders();
}
