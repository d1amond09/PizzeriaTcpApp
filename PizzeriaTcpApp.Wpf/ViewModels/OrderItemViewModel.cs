using System.ComponentModel;
using PizzeriaTcpApp.Shared.Contracts;

namespace PizzeriaTcpApp.Wpf.ViewModels;

public class OrderItemViewModel : INotifyPropertyChanged
{
	public Guid PizzaId { get; set; }
	public string PizzaName { get; set; }
	public decimal Price { get; set; }

	private int _quantity;
	public int Quantity
	{
		get => _quantity;
		set
		{
			_quantity = value;
			OnPropertyChanged(nameof(Quantity));
			OnPropertyChanged(nameof(SubTotal));
		}
	}

	public decimal SubTotal => Price * Quantity;

	public event PropertyChangedEventHandler PropertyChanged;
	protected void OnPropertyChanged(string propertyName)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public OrderItemDtoLess ToDto()
	{
		return new OrderItemDtoLess { PizzaId = this.PizzaId, Quantity = this.Quantity };
	}
}
