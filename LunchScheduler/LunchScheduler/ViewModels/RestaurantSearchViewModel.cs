//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using LunchScheduler.Helpers;
using Microsoft.Toolkit.Uwp.Services.Bing;
using Template10.Common;
using Template10.Mvvm;

namespace LunchScheduler.ViewModels
{
    public class RestaurantSearchViewModel : ViewModelBase
    {
        #region Fields

        private readonly ApplicationDataContainer localSettings;
        private ObservableCollection<BingResult> searchResults;
        private BingResult selectedSearchResult;
        private bool isBusy;
        private string isBusyMessage;
        private string searchQuery;
        private Visibility searchOverlayVisibility;

        private bool saveSelection;
        private bool isWebViewLoading;
        private DelegateCommand toggleWebViewBusyCommand;

        #endregion

        public RestaurantSearchViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                SearchResults = DesignTimeHelpers.GenerateSampleBingResults();
                SelectedSearchResult = SearchResults.FirstOrDefault();
                return;
            }

            localSettings = ApplicationData.Current.LocalSettings;
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

        public Visibility SearchOverlayVisibility
        {
            get { return searchOverlayVisibility; }
            set { Set(ref searchOverlayVisibility, value); }
        }

        public string SearchQuery
        {
            get { return searchQuery; }
            set { Set(ref searchQuery, value); }
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

        public bool IsWebViewLoading
        {
            get { return isWebViewLoading; }
            set { Set(ref isWebViewLoading, value); }
        }

        #endregion

        #region Commands

        public DelegateCommand ToggleWebViewBusyCommand => toggleWebViewBusyCommand ?? (toggleWebViewBusyCommand = new DelegateCommand(() =>
        {
            IsWebViewLoading = !IsWebViewLoading;
        }));

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
                SearchOverlayVisibility = Visibility.Collapsed;
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

        public void OkayButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Toggle this flag so that we save the selected result before navigating
            saveSelection = true;
            BootStrapper.Current.NavigationService.GoBack();
        }

        public void ResetSearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            SearchOverlayVisibility = Visibility.Visible;
            SearchResults.Clear();
            SelectedSearchResult = null;
        }

        public async void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                await SearchAsync(SearchQuery);
            }
        }

        public void SearchBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null)
                return;

            SearchQuery = tb.Text;
        }
        
        public async void SearchBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            // This allows the user to start a search by using the Enter Key
            if (e.Key != VirtualKey.Accept && e.Key != VirtualKey.Enter)
                return;

            if (!string.IsNullOrEmpty(SearchQuery))
                await SearchAsync(SearchQuery);
        }

        public override Task OnNavigatedFromAsync(IDictionary<string, object> pageState, bool suspending)
        {
            // because we're navigating backwards and cannot pass a parameter
            // Store the selected value into localsettings as a composite setting

            if (saveSelection)
            {
                if (localSettings != null)
                {
                    localSettings.Values["SelectedBingResult"] = new ApplicationDataCompositeValue
                    {
                        ["title"] = SelectedSearchResult.Title,
                        ["link"] = SelectedSearchResult.Link
                    }; 
                }
            }
            else
            {
                localSettings.Values["SelectedBingResult"] = null;
            }

            return base.OnNavigatedFromAsync(pageState, suspending);
        }
        
        #endregion
    }
}
