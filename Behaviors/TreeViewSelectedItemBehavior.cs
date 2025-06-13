using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace GM4ManagerWPF.Behaviors
{
    /// <summary>
    /// Behavior to synchronize the selected item of a TreeView with a property in the ViewModel.
    /// </summary>
    /// <remarks>
    /// This behavior allows for two-way binding of the selected item in a TreeView.
    /// It handles programmatic changes to avoid recursive updates.
    /// </remarks>
    /// <example>
    /// Usage: Attach this behavior to a TreeView and bind the SelectedItem property to a ViewModel property.
    /// </example>
    /// <seealso cref="TreeViewSelectedCommandBehavior"/>
    /// <seealso cref="TreeViewSelectedItemBehavior"/>
    /// </remarks>
    public static class TreeViewSelectedItemBehavior
    {
        private static bool _isProgrammaticChange = false;

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItem",
                typeof(object),
                typeof(TreeViewSelectedItemBehavior),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedItemChanged));

        public static object GetSelectedItem(DependencyObject obj) => obj.GetValue(SelectedItemProperty);
        public static void SetSelectedItem(DependencyObject obj, object value) => obj.SetValue(SelectedItemProperty, value);

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TreeView treeView)
            {
                treeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
                treeView.SelectedItemChanged += TreeView_SelectedItemChanged;

                if (e.NewValue != null)
                {
                    _isProgrammaticChange = true;
                    SelectItem(treeView, e.NewValue);
                    _isProgrammaticChange = false;
                }
            }
        }

        private static void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (sender is TreeView treeView && !_isProgrammaticChange)
            {
                Debug.WriteLine($"[Behavior] SelectedItem changed: {e.NewValue?.GetType().Name}");
                SetSelectedItem(treeView, e.NewValue);
            }
        }

        private static bool SelectItem(ItemsControl parent, object targetItem)
        {
            if (parent == null || targetItem == null)
                return false;

            foreach (var item in parent.Items)
            {
                var treeViewItem = (TreeViewItem)parent.ItemContainerGenerator.ContainerFromItem(item);
                if (treeViewItem == null)
                    continue;

                if (item == targetItem)
                {
                    treeViewItem.IsSelected = true;
                    treeViewItem.BringIntoView();
                    return true;
                }

                if (SelectItem(treeViewItem, targetItem))
                {
                    treeViewItem.IsExpanded = true;
                    return true;
                }
            }

            return false;
        }
    }
}