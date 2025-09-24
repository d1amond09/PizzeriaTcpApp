using System.Windows;
using PizzeriaTcpApp.Wpf.Models;

namespace PizzeriaTcpApp.Wpf.Views;

public partial class EditPizzaWindow : Window
{
	public Pizza EditedPizza { get; private set; }

	public EditPizzaWindow(Pizza pizzaToEdit)
	{
		InitializeComponent();
		EditedPizza = pizzaToEdit;
		this.DataContext = EditedPizza;
	}

	private void SaveButton_Click(object sender, RoutedEventArgs e)
	{
		this.DialogResult = true;
		this.Close();
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		this.DialogResult = false;
		this.Close();
	}
}
