using GM4ManagerWPF.Localization;
using GM4ManagerWPF.Models;
using System;
using System.Globalization;
using System.Security.AccessControl;
using System.Windows.Data;

namespace GM4ManagerWPF.Converters
{
    public class PermissionToTooltipConverter : IValueConverter
    {
        private static readonly ResourceService Res = ResourceService.Instance;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PermissionInfo permission)
            {
                if (permission.Rights.HasFlag(FileSystemRights.FullControl))
                    return Res["ttReadWrite"];
                if (permission.CanModify)
                    return Res["ttReadWrite"];
                if (permission.CanReadExecute)
                    return Res["ttReadOnly"];
                return "None";
            }

            return "No info";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
