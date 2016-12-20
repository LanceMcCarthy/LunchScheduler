using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.Toolkit.Uwp.Services.Bing;

namespace LunchScheduler.Dialogs
{
    public sealed partial class RestaurantSearchDialog : ContentDialog
    {
        private ObservableCollection<BingResult> SearchResults { get; set; }

        public static readonly DependencyProperty SelectedResultProperty = DependencyProperty.Register(
            "SelectedResult", typeof(BingResult), typeof(RestaurantSearchDialog), new PropertyMetadata(default(BingResult)));

        public BingResult SelectedResult
        {
            get { return (BingResult) GetValue(SelectedResultProperty); }
            set { SetValue(SelectedResultProperty, value); }
        }

        public RestaurantSearchDialog()
        {
            this.InitializeComponent();
            SearchResults = new ObservableCollection<BingResult>();
        }

        private async Task SearchAsync(string searchTerm)
        {
            try
            {
                BusyOverlay.Visibility = Visibility.Visible;
                BusyIndicator.IsActive = true;

                SearchResults.Clear();

                var searchConfig = new BingSearchConfig
                {
                    Country = BingCountry.UnitedStates,
                    Language = BingLanguage.English,
                    Query = searchTerm,
                    QueryType = BingQueryType.Search
                };

                var result = await BingService.Instance.RequestAsync(searchConfig, 50);

                if (result == null)
                    return;

                foreach (var bingResult in result)
                {
                    SearchResults.Add(bingResult);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SearchAsync Exception: {ex}");
            }
            finally
            {
                BusyOverlay.Visibility = Visibility.Collapsed;
                BusyIndicator.IsActive = false;
            }
        }


        private async void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchBox?.Text))
                await SearchAsync(SearchBox.Text);
        }

        private void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                SearchBox.BorderBrush = new SolidColorBrush(Colors.Red);
                SearchButton.IsEnabled = false;
            }
            else
            {
                SearchBox.BorderBrush = new SolidColorBrush(Colors.Green);
                SearchButton.IsEnabled = true;
            }
        }

        private void ResultsListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e?.AddedItems?.Count > 0)
            {
                IsPrimaryButtonEnabled = true;
                SelectedResult = e.AddedItems.FirstOrDefault() as BingResult;
            }
            else
            {
                IsPrimaryButtonEnabled = false;
                SelectedResult = null;
            }
        }
        
        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
