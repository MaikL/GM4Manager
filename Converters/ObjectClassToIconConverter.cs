using System.Globalization;
using System.Windows.Data;

namespace GM4ManagerWPF.Converters
{
    public class ObjectClassToIconConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is string objectClass)
            {
                return objectClass.ToLower() switch
                {
                    "user" => "👤",
                    "group" => "👥",
                    _ => "❓"
                };
            }
            return "❓";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
