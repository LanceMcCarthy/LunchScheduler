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
using Windows.ApplicationModel.Calls;
using Windows.ApplicationModel.Email;
using Windows.Foundation.Metadata;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Models;
using LunchScheduler.Helpers;
using Template10.Common;
using Template10.Mvvm;

namespace LunchScheduler.ViewModels
{
    public class GuestDetailViewModel : ViewModelBase
    {
        private ObservableCollection<LunchAppointment> allAppointments;
        private bool isBusy;
        private string isBusyMessage;
        private LunchGuest selectedGuest;
        private ObservableCollection<LunchAppointment> invitedAppointments;
        private Visibility callButtonVisibility;
        private Visibility emailButtonVisibility;

        public GuestDetailViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                CallButtonVisibility = Visibility.Collapsed;
                EmailButtonVisibility = Visibility.Visible;
                SelectedGuest = DesignTimeHelpers.GenerateSampleGuest();
                InvitedAppointments = DesignTimeHelpers.GenerateSampleAppointments();
            }
        }

        public LunchGuest SelectedGuest
        {
            get { return selectedGuest; }
            set { Set(ref selectedGuest, value); }
        }

        public ObservableCollection<LunchAppointment> InvitedAppointments
        {
            get { return invitedAppointments ?? (invitedAppointments = new ObservableCollection<LunchAppointment>()); }
            set { Set(ref invitedAppointments, value); }
        }

        public Visibility CallButtonVisibility
        {
            get { return callButtonVisibility; }
            set { Set(ref callButtonVisibility, value); }
        }

        public Visibility EmailButtonVisibility
        {
            get { return emailButtonVisibility; }
            set { Set(ref emailButtonVisibility, value); }
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

        #region Methods

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            try
            {
                UpdateStatus("loading...");

                var guest = parameter as LunchGuest;

                if (guest != null)
                {
                    SelectedGuest = guest;

                    // If the user is on a phone and there are phone numbers available
                    if (ApiInformation.IsApiContractPresent("Windows.ApplicationModel.Calls.CallsPhoneContract", 1, 0) &&
                        guest.PhoneNumbers.Any())
                    {
                        CallButtonVisibility = Visibility.Visible;
                    }

                    // If there are any email addresses, show the email appbar button
                    if (guest.EmailAddresses.Any())
                    {
                        EmailButtonVisibility = Visibility.Visible;
                    }

                    UpdateStatus("loading appointments...");

                    // Get all the appointments the guest is invited to
                    allAppointments = await FileHelpers.LoadLunchAppointments();

                    var invitedTo = allAppointments.Where(a => a.Guests.Contains(guest));

                    foreach (var lunch in invitedTo)
                    {
                        InvitedAppointments.Add(lunch);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatedToAsync Exception: {ex}");
                throw;
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

        public async void DeleteContact_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedGuest == null)
                return;

            try
            {
                UpdateStatus("deleting guest...");

                var md = new MessageDialog("Deleting this guest will also remove them from all lunches. Are you sure you want to delete?", "Delete Guest?");
                md.Commands.Add(new UICommand("delete"));
                md.Commands.Add(new UICommand("cancel"));
                var dialogResult = await md.ShowAsync();

                if (dialogResult.Label == "cancel")
                    return;

                // If the guest isnt in any other appointments, remove the thumbnail from storage
                var result = allAppointments.Where(a => a.Guests.Any(g => g == SelectedGuest));

                if (!result.Any())
                {
                    UpdateStatus($"deleting {SelectedGuest.FullName}'s photo");
                    await FileHelpers.DeleteThumbnailAsync(SelectedGuest.ThumbnailUri);
                }

                if (BootStrapper.Current.NavigationService.CanGoBack)
                    BootStrapper.Current.NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteContact_OnClick Exception: {ex}");
            }
            finally
            {
                UpdateStatus("", false);
            }
        }

        public async void CallContact_OnClick(object sender, RoutedEventArgs e)
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

        public async void EmailContact_OnClick(object sender, RoutedEventArgs e)
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
