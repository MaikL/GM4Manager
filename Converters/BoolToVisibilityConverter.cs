using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GM4ManagerWPF.Converters
{
    /// <summary>
    /// Converts a boolean value to a Visibility value.
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }
        /// <summary>
        /// Converts a boolean value to a Visibility value.
        /// </summary>
        /// <param name="value">The boolean value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A Visibility value based on the boolean input.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = (bool)value;
            if (Invert)
            {
                flag = !flag;
            }

            return flag ? Visibility.Visible : Visibility.Collapsed;
        }
        /// <summary>
        /// Converts a Visibility value back to a boolean value.
        /// </summary>
        /// <param name="value">The Visibility value to convert.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>A boolean value based on the Visibility input.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
