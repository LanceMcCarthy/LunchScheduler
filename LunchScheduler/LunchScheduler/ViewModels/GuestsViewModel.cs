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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Common;
using LunchScheduler.Data.Models;
using LunchScheduler.Helpers;
using LunchScheduler.Views;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using Template10.Common;

namespace LunchScheduler.ViewModels
{
    public class GuestsViewModel : Template10.Mvvm.ViewModelBase
    {
        private ObservableCollection<LunchGuest> currentGuests;
        private bool isBusy;
        private string isBusyMessage;

        public GuestsViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                CurrentGuests = DesignTimeHelpers.GenerateSampleGuests();
                return;
            }
        }

        #region Properties

        public ObservableCollection<LunchGuest> CurrentGuests
        {
            get { return currentGuests ?? (currentGuests = new ObservableCollection<LunchGuest>()); }
            set { Set(ref currentGuests, value); }
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

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            try
            {
                UpdateStatus("loading...");

                var lunchAppointments = await LoadLunchAppointments();

                var guests = lunchAppointments.Where(a => a.Guests.Count > 0).SelectMany(a => a.Guests);

                foreach (var lunchGuest in guests)
                {
                    if (CurrentGuests.All(g => g.Id != lunchGuest.Id))
                        CurrentGuests.Add(lunchGuest);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GuestsViewModel Init Exception: {ex}");
            }
            finally
            {
                UpdateStatus("", false);
            }
        }

        public async Task<ObservableCollection<LunchAppointment>> LoadLunchAppointments()
        {
            try
            {
                UpdateStatus("loading lunch appointments...");

                // UWP Community Toolkit
                var json = await StorageFileHelper.ReadTextFromLocalFileAsync(Constants.LunchAppointmentsFileName);

                Debug.WriteLine($"--Load-- Lunch Appointments JSON:\r\n{json}");

                var appointments = JsonConvert.DeserializeObject<ObservableCollection<LunchAppointment>>(json);

                return appointments;
            }
            catch (FileNotFoundException fnfException)
            {
                Debug.WriteLine($"LoadLunchAppointments FileNotFoundException: {fnfException}");
                return new ObservableCollection<LunchAppointment>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadLunchAppointments Exception: {ex}");
                return new ObservableCollection<LunchAppointment>();
            }
            finally
            {
               UpdateStatus("", false);
            }
        }

        /// <summary>
        /// Shows busy indicator
        /// </summary>
        /// <param name="message">busy message to show</param>
        /// <param name="showBusyIndicator">toggles the busy indicator's visibility</param>
        private void UpdateStatus(string message, bool showBusyIndicator = true)
        {
            IsBusy = showBusyIndicator;
            IsBusyMessage = message;
        }

        #endregion

        #region Event handlers

        public void GuestsGridViewView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems?.Count > 0)
            {
                var guest = e.AddedItems.FirstOrDefault() as LunchGuest;
                if(guest != null)
                    BootStrapper.Current.NavigationService.Navigate(typeof(GuestDetailPage), guest);
            }
        }

        #endregion
    }
}
