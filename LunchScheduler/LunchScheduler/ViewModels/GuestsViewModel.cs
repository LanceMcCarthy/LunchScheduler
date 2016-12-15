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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Calls;
using Windows.ApplicationModel.Contacts;
using Windows.ApplicationModel.Email;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LunchScheduler.Commands;
using LunchScheduler.Data.Common;
using LunchScheduler.Data.Models;
using LunchScheduler.Helpers;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;

namespace LunchScheduler.ViewModels
{
    public class GuestsViewModel : ViewModelBase
    {
        private ObservableCollection<LunchGuest> currentGuests;
        private LunchGuest selectedGuest;

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
            set { SetProperty(ref currentGuests, value); }
        }

        public LunchGuest SelectedGuest
        {
            get
            {
                if (selectedGuest != null)
                    return selectedGuest;

                selectedGuest = CurrentGuests.FirstOrDefault();
                return selectedGuest;
            }
            set { SetProperty(ref selectedGuest, value); }
        }
        
        #endregion


        #region Methods

        public override async Task<bool> Init()
        {
            try
            {
                var lunchAppointments = await LoadLunchAppointments();

                var guests = lunchAppointments.Where(a => a.Guests.Count > 0).SelectMany(a => a.Guests);

                foreach (var lunchGuest in guests)
                {
                    if (CurrentGuests.All(g => g.Id != lunchGuest.Id))
                        CurrentGuests.Add(lunchGuest);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GuestsViewModel Init Exception: {ex}");
                return false;
            }
        }

        public async Task<ObservableCollection<LunchAppointment>> LoadLunchAppointments()
        {
            try
            {
                await UpdateBusyStatusAsync("loading lunch appointments...");

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
                await UpdateBusyStatusAsync("", false);
            }
        }

        #endregion

        #region x:Bind-able event handlers

        public void MasterDetailsView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems == null)
                return;

            if (e.AddedItems.Count > 0)
            {
                SelectedGuest = e.AddedItems.FirstOrDefault() as LunchGuest;
            }
            else
            {
                SelectedGuest = null;
            }
        }
        public async void CallSelectedContact_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedGuest == null || SelectedGuest.PhoneNumbers.Count == 0)
                return;

            if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.Calls.CallsPhoneContract", 1, 0))
            {
                PhoneCallManager.ShowPhoneCallUI(SelectedGuest.PhoneNumbers[0].Number, SelectedGuest.FullName);
            }
            else
            {
                await new MessageDialog("Unfortunately, your device cannot make phone calls.", "No Phone Line").ShowAsync();
            }
        }

        public async void EmailSelectedContact_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedGuest == null || SelectedGuest.EmailAddresses.Count == 0)
                return;

            var emailMessage = new EmailMessage();

            var email = SelectedGuest.EmailAddresses.FirstOrDefault();

            if (email != null)
            {
                var emailRecipient = new EmailRecipient(email.Address);
                emailMessage.To.Add(emailRecipient);
            }

            await EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }
        

        #endregion
    }
}
