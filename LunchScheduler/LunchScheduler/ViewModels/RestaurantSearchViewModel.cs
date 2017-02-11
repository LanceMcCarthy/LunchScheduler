using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Helpers;
using Microsoft.Toolkit.Uwp.Services.Bing;
using Template10.Mvvm;

namespace LunchScheduler.ViewModels
{
    public class RestaurantSearchViewModel : ViewModelBase
    {
        #region Fields

        private ObservableCollection<BingResult> searchResults;
        private BingResult selectedSearchResult;
        private bool isBusy;
        private string isBusyMessage;
        private string searchQuery;

        #endregion

        public RestaurantSearchViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                SearchResults = DesignTimeHelpers.GenerateSampleBingResults();
                SelectedSearchResult = SearchResults.FirstOrDefault();
            }
        }

        #region Properties

        public ObservableCollection<BingResult> SearchResults
        {
            get { return searchResults ?? (searchResults = new ObservableCollection<BingResult>()); }
            set { Set(ref searchResults, value); }
        }

        public BingResult SelectedSearchResult
        {
            get { return selectedSearchResult; }
            set { Set(ref selectedSearchResult, value); }
        }

        public string SearchQuery
        {
            get { return searchQuery; }
            set
            {
                Set(ref searchQuery, value);
            }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { Set(ref isBusy, value); }
        }

        public string IsBusyMessage
        {
            get { return isBusyMessage; }
            set { Set(ref isBusyMessage, value); }
        }

        #endregion

        #region Methods

        private async Task SearchAsync(string searchTerm)
        {
            try
            {
                IsBusy = true;
                IsBusyMessage = $"searching {searchTerm}...";

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

                SelectedSearchResult = SearchResults.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SearchAsync Exception: {ex}");
            }
            finally
            {
                IsBusy = false;
                IsBusyMessage = "";
            }
        }

        #endregion

        #region Event handlers

        public async void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchQuery))
                await SearchAsync(SearchQuery);
        }

        public void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null)
                return;

            SearchQuery = tb.Text;

            tb.BorderBrush = string.IsNullOrEmpty(tb.Text) ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
        }

        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            return base.OnNavigatedFromAsync(pageState, suspending);
        }

        public override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            return base.OnNavigatedToAsync(parameter, mode, state);
        }

        #endregion

    }
}
