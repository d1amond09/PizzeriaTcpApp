using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using PizzeriaTcpApp.Shared.Contracts;
using PizzeriaTcpApp.Wpf.Models;
using PizzeriaTcpApp.Wpf.ViewModels;
using PizzeriaTcpApp.Wpf.Views;
using TcpRestNetworking;

namespace PizzeriaTcpApp.Wpf;

public partial class MainWindow : Window
{
	private readonly RestOverTcpClient _client = new RestOverTcpClient("127.0.0.1", 8080);

	private ObservableCollection<Pizza> _menu = new ObservableCollection<Pizza>();
	private ObservableCollection<OrderItemViewModel> _cart = new ObservableCollection<OrderItemViewModel>();
	private Dictionary<string, string> _availableReports = new Dictionary<string, string>();
	private ObservableCollection<OrderDto> _orders = new ObservableCollection<OrderDto>();

	public MainWindow()
	{
		InitializeComponent();
		Loaded += MainWindow_Loaded;
		OrdersListView.ItemsSource = _orders;
		OrderStatusComboBox.ItemsSource = new[] { "Принят", "Готовится", "Доставлен", "Отменен" };
		PizzaToAddComboBox.ItemsSource = _menu;
	}

	private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		MenuForOrderListBox.ItemsSource = _menu;
		MenuDataGrid.ItemsSource = _menu;
		CartListView.ItemsSource = _cart;

		SetupReports();

		await LoadMenuAsync();
	}

	private async Task LoadMenuAsync()
	{
		try
		{
			var pizzas = await _client.GetAsync<List<Pizza>>("/api/menu/pizzas");
			//MessageBox.Show(string.Join(',', pizzas.Select(x => x.Name)));
			_menu.Clear();
			foreach (var pizza in pizzas)
			{
				_menu.Add(pizza);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Не удалось загрузить меню: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	private async void AddPizzaButton_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(NewPizzaNameTextBox.Text) ||
			string.IsNullOrWhiteSpace(NewPizzaIngredientsTextBox.Text))
		{
			MessageBox.Show("Название и ингредиенты не могут быть пустыми.", "Ошибка ввода");
			return;
		}
		if (!decimal.TryParse(NewPizzaPriceTextBox.Text, out var price) || price <= 0)
		{
			MessageBox.Show("Введите корректную цену.", "Ошибка ввода");
			return;
		}

		var newPizza = new Pizza
		{
			Name = NewPizzaNameTextBox.Text,
			Ingredients = NewPizzaIngredientsTextBox.Text.Split(',')
				.Select(s => s.Trim())
				.ToList(),
			Price = price
		};

		try
		{
			var createdPizza = await _client.PostAsync<Pizza>("/api/menu/pizzas", newPizza);
			MessageBox.Show($"Пицца '{createdPizza.Name}' успешно добавлена в меню!", "Успех");

			NewPizzaNameTextBox.Clear();
			NewPizzaIngredientsTextBox.Clear();
			NewPizzaPriceTextBox.Clear();

			await LoadMenuAsync();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Не удалось добавить пиццу: {ex.Message}", "Ошибка");
		}
	}

	private async void EditPizzaButton_Click(object sender, RoutedEventArgs e)
	{
		if (MenuDataGrid.SelectedItem is not Pizza selectedPizza)
		{
			MessageBox.Show("Пожалуйста, выберите пиццу из списка для редактирования.", "Внимание");
			return;
		}

		var pizzaClone = new Pizza
		{
			Id = selectedPizza.Id,
			Name = selectedPizza.Name,
			Price = selectedPizza.Price,
			Ingredients = [.. selectedPizza.Ingredients]
		};

		var editWindow = new EditPizzaWindow(pizzaClone);
		var result = editWindow.ShowDialog();

		if (result == true)
		{
			try
			{
				var updatedPizza = editWindow.EditedPizza;

				await _client.PutAsync($"/api/menu/pizzas/{updatedPizza.Id}", updatedPizza);

				MessageBox.Show($"Пицца '{updatedPizza.Name}' успешно обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

				await LoadMenuAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Не удалось обновить пиццу: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}

	private async void DeletePizzaButton_Click(object sender, RoutedEventArgs e)
	{
		if (MenuDataGrid.SelectedItem is not Pizza selectedPizza)
		{
			MessageBox.Show("Выберите пиццу для удаления.");
			return;
		}

		if (MessageBox.Show($"Удалить '{selectedPizza.Name}'?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
		{
			try
			{
				await _client.DeleteAsync($"/api/menu/pizzas/{selectedPizza.Id}");
				MessageBox.Show("Пицца удалена!");
				await LoadMenuAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка удаления: {ex.Message}");
			}
		}
	}

	private void RefreshMenuButton_Click(object sender, RoutedEventArgs e)
	{
		LoadMenuAsync();
	}

	#region Order Logic
	private async void RefreshOrdersButton_Click(object sender, RoutedEventArgs e)
	{
		await LoadOrdersAsync();
	}

	private async Task LoadOrdersAsync()
	{
		try
		{
			var orders = await _client.GetAsync<List<OrderDto>>("/api/orders");
			_orders.Clear();
			foreach (var order in orders)
			{
				_orders.Add(order);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Не удалось загрузить заказы: {ex.Message}");
		}
	}

	private void OrdersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (OrdersListView.SelectedItem is OrderDto selectedOrder)
		{
			OrderDetailsPanel.IsEnabled = true;
			OrderCustomerNameTextBox.Text = selectedOrder.CustomerName;
			OrderDatePicker.SelectedDate = selectedOrder.OrderDate; 
			OrderStatusComboBox.SelectedItem = selectedOrder.Status;
			OrderItemsListView.ItemsSource = new ObservableCollection<OrderItemDto>(selectedOrder.Items);
		}
		else
		{
			OrderDetailsPanel.IsEnabled = false;
			OrderItemsListView.ItemsSource = null;
		}
	}

	private async void SaveChangesToOrderButton_Click(object sender, RoutedEventArgs e)
	{
		if (OrdersListView.SelectedItem is not OrderDto selectedOrder) return;

		selectedOrder.CustomerName = OrderCustomerNameTextBox.Text;
		selectedOrder.Status = OrderStatusComboBox.SelectedItem as string;
		selectedOrder.OrderDate = OrderDatePicker.SelectedDate ?? DateTime.Now; 

		var updatedItems = OrderItemsListView.ItemsSource as ObservableCollection<OrderItemDto>;
		selectedOrder.Items = updatedItems.ToList();

		try
		{
			await _client.PutAsync($"/api/orders/{selectedOrder.Id}", selectedOrder);
			MessageBox.Show("Заказ обновлен!");
			await LoadOrdersAsync();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Ошибка обновления заказа: {ex.Message}");
		}
	}

	private async void DeleteOrderButton_Click(object sender, RoutedEventArgs e)
	{
		if (OrdersListView.SelectedItem is not OrderDto selectedOrder) return;

		if (MessageBox.Show($"Удалить заказ №{selectedOrder.Id}?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
		{
			try
			{
				await _client.DeleteAsync($"/api/orders/{selectedOrder.Id}");
				MessageBox.Show("Заказ удален!");
				OrderDetailsPanel.IsEnabled = false;
				await LoadOrdersAsync();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Ошибка удаления заказа: {ex.Message}");
			}
		}
	}

	private void AddToOrderButton_Click(object sender, RoutedEventArgs e)
	{
		if (MenuForOrderListBox.SelectedItem is not Pizza selectedPizza)
		{
			MessageBox.Show("Пожалуйста, выберите пиццу из списка.", "Внимание");
			return;
		}

		if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
		{
			MessageBox.Show("Пожалуйста, введите корректное количество (целое число больше 0).", "Ошибка ввода");
			return;
		}

		var existingItem = _cart.FirstOrDefault(item => item.PizzaId == selectedPizza.Id);
		if (existingItem != null)
		{
			existingItem.Quantity += quantity;
		}
		else
		{
			_cart.Add(new OrderItemViewModel
			{
				PizzaId = selectedPizza.Id,
				PizzaName = selectedPizza.Name,
				Price = selectedPizza.Price,
				Quantity = quantity
			});
		}

		UpdateTotalPrice();
	}

	private void AddPizzaToExistingOrder_Click(object sender, RoutedEventArgs e)
	{
		if (PizzaToAddComboBox.SelectedItem is not Pizza pizzaToAdd ||
			OrderItemsListView.ItemsSource is not ObservableCollection<OrderItemDto> currentItems ||
			OrdersListView.SelectedItem is not OrderDto currentOrder)
		{
			return;
		}

		var newItem = new OrderItemDto
		{
			OrderId = currentOrder.Id,
			PizzaId = pizzaToAdd.Id,
			PizzaName = pizzaToAdd.Name,
			Quantity = 1,
			PriceAtTimeOfOrder = pizzaToAdd.Price
		};

		currentItems.Add(newItem);
	}

	private void RemovePizzaFromExistingOrder_Click(object sender, RoutedEventArgs e)
	{
		if (OrderItemsListView.SelectedItem is not OrderItemDto itemToRemove ||
			OrderItemsListView.ItemsSource is not ObservableCollection<OrderItemDto> currentItems)
		{
			return;
		}

		currentItems.Remove(itemToRemove);
	}

	private void UpdateTotalPrice()
	{
		decimal total = _cart.Sum(item => item.SubTotal);
		TotalPriceTextBlock.Text = $"Итого: {total:F2} руб.";
	}

	private async void PlaceOrderButton_Click(object sender, RoutedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(CustomerNameTextBox.Text))
		{
			MessageBox.Show("Пожалуйста, введите имя клиента.", "Ошибка");
			return;
		}
		if (!_cart.Any())
		{
			MessageBox.Show("Ваша корзина пуста.", "Ошибка");
			return;
		}

		var request = new CreateOrderRequest
		{
			CustomerName = CustomerNameTextBox.Text,
			Items = _cart.Select(item => item.ToDto()).ToList()
		};

		try
		{
			var createdOrder = await _client.PostAsync<OrderDto>("/api/orders", request);
			MessageBox.Show($"Заказ №{createdOrder.Id} успешно создан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

			_cart.Clear();
			CustomerNameTextBox.Clear();
			UpdateTotalPrice();
		}
		catch (Exception ex)
		{
			MessageBox.Show($"Не удалось создать заказ: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	#endregion

	#region Reports Logic

	private void SetupReports()
	{
		_availableReports.Add("Самая популярная пицца", "/api/reports/most-popular-pizza");
		_availableReports.Add("Самая прибыльная пицца", "/api/reports/most-profitable-pizza");
		_availableReports.Add("Средний чек", "/api/reports/average-order-value");
		_availableReports.Add("Заказы по статусам", "/api/reports/orders-by-status");
		_availableReports.Add("Общая выручка за период", "/api/reports/total-revenue");

		ReportsComboBox.ItemsSource = _availableReports.Keys;
		ReportsComboBox.SelectedIndex = 0;
	}

	private void ReportsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		var selectedReport = ReportsComboBox.SelectedItem as string;
		DateRangePanel.Visibility = selectedReport == "Общая выручка за период" ? Visibility.Visible : Visibility.Collapsed;
	}

	private async void GenerateReportButton_Click(object sender, RoutedEventArgs e)
	{
		if (ReportsComboBox.SelectedItem is not string selectedReportKey) return;

		var path = _availableReports[selectedReportKey];

		if (selectedReportKey == "Общая выручка за период")
		{
			if (FromDatePicker.SelectedDate == null || ToDatePicker.SelectedDate == null)
			{
				MessageBox.Show("Пожалуйста, выберите начальную и конечную дату.", "Ошибка");
				return;
			}
			path += $"?from={FromDatePicker.SelectedDate:yyyy-MM-dd}&to={ToDatePicker.SelectedDate:yyyy-MM-dd}";
		}

		ReportResultsTextBox.Text = "Загрузка...";
		try
		{
			var result = await _client.GetAsync<JsonElement>(path);

			var jsonOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
			ReportResultsTextBox.Text = JsonSerializer.Serialize(result, jsonOptions);
		}
		catch (Exception ex)
		{
			ReportResultsTextBox.Text = $"Ошибка при формировании отчета:\n{ex.Message}";
		}
	}

	#endregion
}