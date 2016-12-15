using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace LunchScheduler.Views
{
    public sealed partial class ShellPage : Page
    {
        public Frame ContentFrame => contentFrame;
        
        private static List<MenuItem> MenuItems => new List<MenuItem>
        {
            new MenuItem {Icon = Symbol.Calendar, Name = "Overview", PageType = typeof(OverviewPage)},
            new MenuItem {Icon = Symbol.People, Name = "Guests", PageType = typeof(GuestsPage)}
        };

        private static List<MenuItem> OptionsMenuItems => new List<MenuItem>
        {
            new MenuItem {Icon = Symbol.ContactPresence, Name = "Profile", PageType = typeof(ProfilePage)},
            new MenuItem {Icon = Symbol.Setting, Name = "Options", PageType = typeof(OptionsPage)}
        };

        public ShellPage()
        {
            InitializeComponent();

            hamburgerMenuControl.ItemsSource = MenuItems;
            hamburgerMenuControl.OptionsItemsSource = OptionsMenuItems;
        }

        private void OnMenuItemClick(object sender, ItemClickEventArgs e)
        {
            var menuItem = e?.ClickedItem as MenuItem;
            if (menuItem == null)
                return;

            Debug.WriteLine($"HamburgerMenu ItemClicked - Title: {menuItem.Name}");

            // Note: App.ContentFrame is just a global reference to this.contentFrame 
            // for consistency through the app, we'll use the global reference instead
            App.ContentFrame.Navigate(menuItem.PageType);
        }

        private void HamburgerMenuControl_OnLoaded(object sender, RoutedEventArgs e)
        {
            // This navigates to the first page when the app loads
            hamburgerMenuControl.SelectedItem = MenuItems[0];
            App.ContentFrame.Navigate(MenuItems[0].PageType);
        }
    }

    public class MenuItem
    {
        public Symbol Icon { get; set; }
        public string Name { get; set; }
        public Type PageType { get; set; }
    }
}
