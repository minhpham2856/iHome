using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace iHome.UI.Views.Shared
{
	public partial class Sidebar : UserControl
	{
		private List<SidebarMenuItem> _items = new();

		public event Action<SidebarMenuItem>? MenuItemSelected;
		public event Action? LogoutRequested;

		public Sidebar()
		{
			// Load XAML markup and register named controls for code-behind
			InitializeComponent();
		}

		public void SetItems(IEnumerable<SidebarMenuItem> items)
		{
			// Materialize query to List for repeated binding and filtering
			_items = items.ToList();
			// Bind ItemsSource so grid/combo displays BLL list or ICollectionView
			MenuItemsControl.ItemsSource = _items;
		}

		public void SetSelected(string pageName)
		{
			// Iterate collection to update UI, chart series, or CSV rows
			foreach (var item in _items)
			{
				// Assign local/page state inside SetSelected without altering business rules
				item.IsSelected = item.PageName == pageName;
			}
		}

		private void MenuButton_Click(object sender, RoutedEventArgs e)
		{
			// Guard clause: only continue when UI selection, role, or input is valid
			if (sender is Button button && button.DataContext is SidebarMenuItem item)
			{
				// Execute UI step inside MenuButton_Click
				MenuItemSelected?.Invoke(item);
			}
		}

		private void LogoutButton_Click(object sender, RoutedEventArgs e) =>
			LogoutRequested?.Invoke();
	}

	public class SidebarMenuItem : INotifyPropertyChanged
	{
		private bool _isSelected;

		public string Title { get; set; } = string.Empty;
		public string PageName { get; set; } = string.Empty;

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (_isSelected == value)
				{
					return;
				}

				_isSelected = value;
				OnPropertyChanged();
			}
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
