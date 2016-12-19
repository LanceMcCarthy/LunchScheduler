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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Models;
using LunchScheduler.Helpers;
using LunchScheduler.Views;
using Template10.Common;
using Template10.Mvvm;

namespace LunchScheduler.ViewModels
{
    public class LunchesViewModel : ViewModelBase
    {
        private ObservableCollection<LunchAppointment> lunchAppointments;
        private bool isBusy;
        private string isBusyMessage;

        public LunchesViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                LunchAppointments = DesignTimeHelpers.GenerateSampleAppointments();
            }
        }

        #region Properties

        public ObservableCollection<LunchAppointment> LunchAppointments
        {
            get { return lunchAppointments ?? (lunchAppointments = new ObservableCollection<LunchAppointment>()); }
            set { Set(ref lunchAppointments, value); }
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

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode,
            IDictionary<string, object> state)
        {
            try
            {
                UpdateStatus("loading...");

                LunchAppointments = await FileHelpers.LoadLunchAppointments();

                // If the app was launched from a toast notification, the paramater will have the
                // appointment ID (see App.xaml.cs Line 85)
                if (parameter is string)
                {
                    var appointmentId = parameter.ToString();

                    var toastActivatedAppointment = LunchAppointments.FirstOrDefault(a => a.Id == appointmentId);

                    if (toastActivatedAppointment != null)
                    {
                        BootStrapper.Current.NavigationService.Navigate(typeof(LunchDetailPage),
                            toastActivatedAppointment);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatedToAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("");
            }
        }

        /// <summary>
        /// Shows busy indicator. Passing an empty string will hide the busy overlay
        /// </summary>
        /// <param name="message">busy message to show, leave empty to hide the overlay</param>
        private void UpdateStatus(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                IsBusy = false;
                IsBusyMessage = message;
            }
            else
            {
                IsBusy = true;
                IsBusyMessage = message;
            }
        }

        #endregion

        #region Event handlers

        public void UpcomingLunchesGridView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Any())
            {
                var selectedAppointment = e.AddedItems.FirstOrDefault();

                if (selectedAppointment != null)
                    BootStrapper.Current.NavigationService.Navigate(typeof(LunchDetailPage), selectedAppointment);
            }
        }

        public void AddAppointmentAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            BootStrapper.Current.NavigationService.Navigate(typeof(AddLunchPage), LunchAppointments);
        }

        public async void ClearOldAppointmentsAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("cleaning appointments list");

                // Get a list of any appointments that are not in the future
                var oldAppointments = LunchAppointments.Where(a => a.LunchTime < DateTimeOffset.Now).ToList();

                if (!oldAppointments.Any())
                    return;

                foreach (var oldAppointment in oldAppointments)
                {
                    LunchAppointments.Remove(oldAppointment);
                }

                await FileHelpers.SaveLunchAppointments(LunchAppointments);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ClearOldAppointmentsAppBarButton_OnClick Exception {ex}");
            }
            finally
            {
                UpdateStatus("");
            }
        }

        #endregion
    }
}
