using System.Windows;
using System.Windows.Controls;
using PizzeriaTcpApp.Wpf.Models;
using TcpRestNetworking;

namespace PizzeriaTcpApp.Wpf;

public partial class MainWindow : Window
{
	private readonly RestOverTcpClient _client = new ("127.0.0.1", 8888);
	private Pizza? _selectedPizza;

	public MainWindow()
	{
		InitializeComponent();
		Loaded += MainWindow_Loaded;
	}

	private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
	{
		await LoadPizzasAsync();
	}

	private void PizzasListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		_selectedPizza = PizzasListBox.SelectedItem as Pizza;
		PopulateDetails();
	}

	#region CRUD Operations

	private async void AddButton_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (!ValidateInput()) return;

			var newPizza = new Pizza();
			MapFormToPizza(newPizza);

			await _client.PostAsync("/pizzas", newPizza);
			MessageBox.Show("Новая пицца успешно добавлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

			ClearForm();
			await LoadPizzasAsync();
		}
		catch (Exception ex)
		{
			ShowError($"Ошибка при добавлении: {ex.Message}");
		}
	}

	private async void UpdateButton_Click(object sender, RoutedEventArgs e)
	{
		if (_selectedPizza == null)
		{
			MessageBox.Show("Сначала выберите пиццу для обновления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}

		try
		{
			if (!ValidateInput()) return;

			MapFormToPizza(_selectedPizza);
			await _client.PutAsync($"/pizzas/{_selectedPizza.Id}", _selectedPizza);
			MessageBox.Show("Данные пиццы успешно обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

			await LoadPizzasAsync();
		}
		catch (Exception ex)
		{
			ShowError($"Ошибка при обновлении: {ex.Message}");
		}
	}

	private async void DeleteButton_Click(object sender, RoutedEventArgs e)
	{
		if (_selectedPizza == null)
		{
			MessageBox.Show("Сначала выберите пиццу для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}

		var result = MessageBox.Show($"Вы уверены, что хотите удалить '{_selectedPizza.Name}'?",
			"Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

		if (result == MessageBoxResult.Yes)
		{
			try
			{
				await _client.DeleteAsync($"/pizzas/{_selectedPizza.Id}");
				MessageBox.Show("Пицца успешно удалена.", "Успех");

				await LoadPizzasAsync();
				ClearForm();
			}
			catch (Exception ex)
			{
				ShowError($"Ошибка при удалении: {ex.Message}");
			}
		}
	}

	#endregion

	#region Ingredient Management

	private void AddIngredientButton_Click(object sender, RoutedEventArgs e)
	{
		var newIngredient = NewIngredientTextBox.Text.Trim();
		if (!string.IsNullOrEmpty(newIngredient))
		{
			var ingredients = (IngredientsListBox.ItemsSource as IEnumerable<string> ?? []).ToList();
			ingredients.Add(newIngredient);
			IngredientsListBox.ItemsSource = ingredients;
			NewIngredientTextBox.Clear();
		}
	}

	private void RemoveIngredientButton_Click(object sender, RoutedEventArgs e)
	{
		if (IngredientsListBox.SelectedItem is string selectedIngredient)
		{
			var ingredients = (IngredientsListBox.ItemsSource as IEnumerable<string>).ToList();
			ingredients.Remove(selectedIngredient);
			IngredientsListBox.ItemsSource = ingredients;
		}
	}

	#endregion

	#region UI Helper Methods

	private async Task LoadPizzasAsync()
	{
		try
		{
			var pizzas = await _client.GetAsync<IEnumerable<Pizza>>("/pizzas");
			var selectedId = _selectedPizza?.Id;
			PizzasListBox.ItemsSource = pizzas;

			if (selectedId != null)
			{
				PizzasListBox.SelectedItem = pizzas?.FirstOrDefault(p => p.Id == selectedId);
			}
		}
		catch (Exception ex)
		{
			ShowError($"Не удалось загрузить список пицц. Убедитесь, что сервер запущен.\n\nОшибка: {ex.Message}");
		}
	}

	private void PopulateDetails()
	{
		if (_selectedPizza != null)
		{
			DetailsPanel.IsEnabled = true;
			IdTextBox.Text = _selectedPizza.Id.ToString();
			NameTextBox.Text = _selectedPizza.Name;
			PriceTextBox.Text = _selectedPizza.Price.ToString("F2");
			IngredientsListBox.ItemsSource = new List<string>(_selectedPizza.Ingredients);
		}
		else
		{
			ClearForm();
		}
	}

	private void MapFormToPizza(Pizza pizza)
	{
		pizza.Name = NameTextBox.Text;
		pizza.Price = decimal.Parse(PriceTextBox.Text);
		pizza.Ingredients = [.. (IngredientsListBox.ItemsSource as IEnumerable<string> ?? [])];
	}

	private void ClearForm()
	{
		_selectedPizza = null;
		PizzasListBox.SelectedItem = null;
		IdTextBox.Clear();
		NameTextBox.Clear();
		PriceTextBox.Clear();
		IngredientsListBox.ItemsSource = null;
		NewIngredientTextBox.Clear();
		DetailsPanel.IsEnabled = false;
	}

	private bool ValidateInput()
	{
		if (string.IsNullOrWhiteSpace(NameTextBox.Text))
		{
			MessageBox.Show("Поле 'Название' не может быть пустым.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
			return false;
		}
		if (!decimal.TryParse(PriceTextBox.Text, out _))
		{
			MessageBox.Show("Поле 'Цена' должно быть числом.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Error);
			return false;
		}
		return true;
	}

	private void ShowError(string message)
	{
		MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
	}

	private async void RefreshButton_Click(object sender, RoutedEventArgs e)
	{
		await LoadPizzasAsync();
	}

	private void ClearButton_Click(object sender, RoutedEventArgs e)
	{
		PizzasListBox.SelectedItem = null;

		DetailsPanel.IsEnabled = true;
		IdTextBox.Text = "(новый)"; 
	}

	#endregion
}