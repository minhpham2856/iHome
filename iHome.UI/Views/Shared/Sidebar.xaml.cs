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
			InitializeComponent();
		}

		public void SetItems(IEnumerable<SidebarMenuItem> items)
		{
			_items = items.ToList();
			MenuItemsControl.ItemsSource = _items;
		}

		public void SetSelected(string pageName)
		{
			foreach (var item in _items)
			{
				item.IsSelected = item.PageName == pageName;
			}
		}

		private void MenuButton_Click(object sender, RoutedEventArgs e)
		{
			if (sender is Button button && button.DataContext is SidebarMenuItem item)
			{
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
