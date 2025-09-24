using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using PizzeriaTcpApp.Shared.Contracts;
using PizzeriaTcpApp.Wpf.Models;
using PizzeriaTcpApp.Wpf.ViewModels;
using TcpRestNetworking;

namespace PizzeriaTcpApp.Wpf;

public partial class MainWindow : Window
{
	// === ВАЖНО: Клиент подключается только к API Gateway! ===
	private readonly RestOverTcpClient _client = new RestOverTcpClient("127.0.0.1", 8080);

	// Коллекции для привязки к UI
	private ObservableCollection<Pizza> _menu = new ObservableCollection<Pizza>();
	private ObservableCollection<OrderItemViewModel> _cart = new ObservableCollection<OrderItemViewModel>();
	private Dictionary<string, string> _availableReports = new Dictionary<string, string>();

	public MainWindow()
	{
		InitializeComponent();
		Loaded += MainWindow_Loaded;
	}

	private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		// Привязываем источники данных
		MenuForOrderListBox.ItemsSource = _menu;
		MenuDataGrid.ItemsSource = _menu;
		CartListView.ItemsSource = _cart;

		SetupReports();

		// Загружаем меню при старте
		await LoadMenuAsync();
	}

	private async Task LoadMenuAsync()
	{
		try
		{
			var pizzas = await _client.GetAsync<List<Pizza>>("/api/menu/pizzas");
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

	private void RefreshMenuButton_Click(object sender, RoutedEventArgs e)
	{
		LoadMenuAsync();
	}

	#region Order Logic

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

		// Проверяем, есть ли уже такая пицца в корзине
		var existingItem = _cart.FirstOrDefault(item => item.PizzaId == selectedPizza.Id);
		if (existingItem != null)
		{
			// Если есть - просто увеличиваем количество
			existingItem.Quantity += quantity;
		}
		else
		{
			// Если нет - добавляем новую позицию
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

			// Очистка формы
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
		// Показываем/скрываем выбор даты для соответствующего отчета
		DateRangePanel.Visibility = selectedReport == "Общая выручка за период" ? Visibility.Visible : Visibility.Collapsed;
	}

	private async void GenerateReportButton_Click(object sender, RoutedEventArgs e)
	{
		if (ReportsComboBox.SelectedItem is not string selectedReportKey) return;

		var path = _availableReports[selectedReportKey];

		// Если отчет требует диапазон дат, добавляем его в запрос
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
			// Используем JsonElement, так как структура ответа может быть разной
			var result = await _client.GetAsync<JsonElement>(path);

			// Красиво форматируем JSON для вывода
			var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
			ReportResultsTextBox.Text = JsonSerializer.Serialize(result, jsonOptions);
		}
		catch (Exception ex)
		{
			ReportResultsTextBox.Text = $"Ошибка при формировании отчета:\n{ex.Message}";
		}
	}

	#endregion
}