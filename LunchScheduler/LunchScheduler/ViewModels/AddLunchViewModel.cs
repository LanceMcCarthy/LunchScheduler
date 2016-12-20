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
using Windows.ApplicationModel.Contacts;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Models;
using LunchScheduler.Dialogs;
using LunchScheduler.Helpers;
using Template10.Common;
using Template10.Mvvm;

namespace LunchScheduler.ViewModels
{
    public class AddLunchViewModel : ViewModelBase
    {
        private ObservableCollection<LunchAppointment> allAppointments;
        private LunchAppointment lunchToAdd;
        private bool isBusy;
        private string isBusyMessage;

        public AddLunchViewModel()
        {
            if (DesignMode.DesignModeEnabled)
                LunchToAdd = DesignTimeHelpers.GenerateSampleAppointment();
        }

        #region Properties

        public LunchAppointment LunchToAdd
        {
            get { return lunchToAdd ?? (lunchToAdd = new LunchAppointment()); }
            set { Set(ref lunchToAdd, value); }
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

        public override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            try
            {
                UpdateStatus("loading...");

                var appts = parameter as ObservableCollection<LunchAppointment>;

                allAppointments = appts ?? new ObservableCollection<LunchAppointment>();

                return base.OnNavigatedToAsync(parameter, mode, state);

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatedToAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("");
            }

            return base.OnNavigatedToAsync(parameter, mode, state);
        }

        /// <summary>
        /// Adds a new lunch
        /// </summary>
        /// <returns></returns>
        private async Task AddLunchAppointmentAsync()
        {
            try
            {
                UpdateStatus("adding appointment...");

                if (LunchToAdd == null)
                    return;

                allAppointments.Add(LunchToAdd);

                UpdateStatus("sorting appointments...");

                // If there is more than one appointment, sort them by the date
                if (allAppointments.Count > 1)
                {
                    allAppointments = new ObservableCollection<LunchAppointment>(allAppointments.OrderByDescending(a => a.LunchTime).ToList());
                }

                UpdateStatus("saving appointments...");

                // Save updated appointments list to storage
                await FileHelpers.SaveLunchAppointments(allAppointments);

                if (BootStrapper.Current.NavigationService.CanGoBack)
                    BootStrapper.Current.NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddLunchAppointmentAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("");
            }
        }

        /// <summary>
        /// Opens device's contacts flyout so the user can pick a guest from their list of contacts
        /// The contacts's thumbnail stream is also saved to a file for easy databinding
        /// </summary>
        /// <param name="appointment">Lunch appointment to add a guest to</param>
        /// <returns></returns>
        private async Task AddLunchGuestAsync(LunchAppointment appointment)
        {
            try
            {
                UpdateStatus("adding guest to appointment...");

                // User selects a contact from their device
                var userSelectedContact = await new ContactPicker().PickContactAsync();

                // We need to get the full contact info so we have access to the thumbnail
                var contactStore = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AllContactsReadOnly);
                var fullContact = await contactStore.GetContactAsync(userSelectedContact.Id);

                if (fullContact == null)
                    return;

                LunchGuest lunchGuest;

                // If we have already got the guest in the list, return
                if (appointment.Guests.Any())
                {
                    lunchGuest = appointment.Guests.FirstOrDefault(c => c.Id == fullContact.Id);

                    if (lunchGuest != null)
                    {
                        Debug.WriteLine("Contact exists in appointment");
                        await
                            new MessageDialog($"You already have {fullContact.FullName} listed as a guest").ShowAsync();

                        return;
                    }
                }

                // The contact does not exist in the app, lets add them now
                lunchGuest = new LunchGuest
                {
                    Id = fullContact.Id,
                    FullName = fullContact.FullName
                };

                // Add each phone number
                foreach (var contactPhone in fullContact.Phones)
                {
                    lunchGuest.PhoneNumbers.Add(new LunchGuestPhoneNumber
                    {
                        Description = contactPhone.Kind.ToString("G"),
                        Number = contactPhone.Number
                    });
                }

                // Add each email address
                foreach (var contactEmail in fullContact.Emails)
                {
                    lunchGuest.EmailAddresses.Add(new LunchGuestEmailAddress
                    {
                        Description = contactEmail.Kind.ToString("G"),
                        Address = contactEmail.Address
                    });
                }

                UpdateStatus("saving contact photo...");

                // Save the contact's thumnail as a jpg image and get the file path for DataBinding to UIElements
                lunchGuest.ThumbnailUri = await FileHelpers.SaveThumbnailAsync(fullContact);

                appointment.Guests.Add(lunchGuest);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddLunchGuestAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("");
            }
        }

        /// <summary>
        /// Removes a guest and deletes the thumbnail photo
        /// </summary>
        /// <param name="guest">Guest to remove</param>
        /// <returns></returns>
        private async Task RemoveLunchGuest(LunchGuest guest)
        {
            if (guest == null || !LunchToAdd.Guests.Contains(guest))
                return;

            LunchToAdd.Guests.Remove(guest);

            // If the guest isnt in any other appointments, remove the thumbnail from storage
            var result = allAppointments.Where(a => a.Guests.Any(g => g == guest));

            if (!result.Any())
                await FileHelpers.DeleteThumbnailAsync(guest.ThumbnailUri);
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

        public async void SaveLunchToAddButton_OnClick(object sender, RoutedEventArgs e)
        {
            await AddLunchAppointmentAsync();
        }

        public void CancelLunchToAddButton_OnClick(object sender, RoutedEventArgs e)
        {


            if (BootStrapper.Current.NavigationService.CanGoBack)
                BootStrapper.Current.NavigationService.GoBack();
        }

        public async void AddLunchGuestButton_OnClick(object sender, RoutedEventArgs e)
        {
            await AddLunchGuestAsync(LunchToAdd);
        }

        public async void RemoveLunchGuestButton_OnClick(object sender, RoutedEventArgs e)
        {
            var guest = ((Button)sender)?.DataContext as LunchGuest;

            if (guest != null)
                await RemoveLunchGuest(guest);
        }

        public void LunchToAdd_OnDateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            if (e == null)
                return;

            LunchToAdd.LunchTime = new DateTimeOffset(e.NewDate.Year, e.NewDate.Month, e.NewDate.Day,
                LunchToAdd.LunchTime.Hour, LunchToAdd.LunchTime.Minute, 0, LunchToAdd.LunchTime.Offset);
        }

        public void LunchToAdd_OnTimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            if (e == null)
                return;

            LunchToAdd.LunchTime = new DateTimeOffset(LunchToAdd.LunchTime.Year, LunchToAdd.LunchTime.Month,
                LunchToAdd.LunchTime.Day, e.NewTime.Hours, e.NewTime.Minutes, 0, LunchToAdd.LunchTime.Offset);
        }

        public async void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            var searchDialog = new RestaurantSearchDialog();
            await searchDialog.ShowAsync();

            if (searchDialog?.SelectedResult != null)
            {
                LunchToAdd.Location.Name = searchDialog?.SelectedResult.Title;
                LunchToAdd.Location.Address = searchDialog.SelectedResult.Link;
            }
        }

        #endregion
    }
}
