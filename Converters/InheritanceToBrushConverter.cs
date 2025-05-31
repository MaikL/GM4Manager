using System.Drawing;
using System.Globalization;
using System.Windows.Data;

namespace GM4ManagerWPF.Converters
{
    public class InheritanceToBrushConverter : IValueConverter
    {
        public Brush? InheritanceDisabledBrush { get; set; }
        public Brush? InheritanceEnabledBrush { get; set; }

        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDisabled)
            {
                return isDisabled ? InheritanceDisabledBrush : InheritanceEnabledBrush;
            }
            return InheritanceEnabledBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


}
