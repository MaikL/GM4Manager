using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GM4ManagerWPF.Behaviors
{
    public static class TreeViewSelectedCommandBehavior
    {
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached(
                "Command",
                typeof(ICommand),
                typeof(TreeViewSelectedCommandBehavior),
                new PropertyMetadata(null, OnCommandChanged));

        public static ICommand GetCommand(DependencyObject obj) => (ICommand)obj.GetValue(CommandProperty);

        public static void SetCommand(DependencyObject obj, ICommand value) => obj.SetValue(CommandProperty, value);

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeViewItem item)
            {
                item.Selected -= OnSelected;
                item.Selected += OnSelected;
            }
        }

        private static void OnSelected(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is object dc)
            {
                ICommand? command = GetCommand(item);
                if (command != null && command.CanExecute(dc))
                {
                    command.Execute(dc);
                }
            }
        }
    }
}