using System.Globalization;
using System.Windows.Data;

namespace PizzeriaTcpApp.Wpf.Converters;

public class IngredientsConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is List<string> ingredients)
		{
			return string.Join(", ", ingredients);
		}
		return string.Empty;
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is string str)
		{
			return str.Split(',')
					  .Select(s => s.Trim())
					  .Where(s => !string.IsNullOrEmpty(s))
					  .ToList();
		}
		return new List<string>();
	}
}
