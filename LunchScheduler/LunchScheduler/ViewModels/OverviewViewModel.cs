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
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Contacts;
using Windows.Storage;
using Windows.Storage.Streams;
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
    public class OverviewViewModel : ViewModelBase
    {
        private ObservableCollection<LunchAppointment> lunchAppointments;
        private ObservableCollection<LunchGuest> currentLunchGuests;
        private ObservableCollection<LunchGuest> selectedCurrentGuests;

        private Visibility addAppointmentOverlayVisibility = Visibility.Collapsed;
        private LunchAppointment lunchToAdd;
        private LunchAppointment selectedLunch;
        private LunchGuest selectedGuest;
        private DelegateCommand<LunchGuest> removeLunchToAddGuestCommand;
        
        public OverviewViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                LunchAppointments = DesignTimeHelpers.GenerateSampleAppointments();
                CurrentLunchGuests = DesignTimeHelpers.GenerateSampleGuests();
                SelectedLunch = LunchAppointments[0];
                return;
            }
        }

        #region Properties

        public ObservableCollection<LunchAppointment> LunchAppointments
        {
            get { return lunchAppointments ?? (lunchAppointments = new ObservableCollection<LunchAppointment>()); }
            set { SetProperty(ref lunchAppointments, value); }
        }

        public ObservableCollection<LunchGuest> CurrentLunchGuests
        {
            get { return currentLunchGuests ?? (currentLunchGuests = new ObservableCollection<LunchGuest>()); }
            set { SetProperty(ref currentLunchGuests, value); }
        }

        public ObservableCollection<LunchGuest> SelectedCurrentGuests
        {
            get { return selectedCurrentGuests ?? (selectedCurrentGuests = new ObservableCollection<LunchGuest>()); }
            set { SetProperty(ref selectedCurrentGuests, value); }
        }

        public LunchAppointment LunchToAdd
        {
            get { return lunchToAdd ?? (lunchToAdd = new LunchAppointment()); }
            set { SetProperty(ref lunchToAdd, value); }
        }

        public LunchAppointment SelectedLunch
        {
            get { return selectedLunch; }
            set { SetProperty(ref selectedLunch, value); }
        }

        public LunchGuest SelectedGuest
        {
            get { return selectedGuest; }
            set { SetProperty(ref selectedGuest, value); }
        }

        public Visibility AddAppointmentOverlayVisibility
        {
            get { return addAppointmentOverlayVisibility; }
            set { SetProperty(ref addAppointmentOverlayVisibility, value); }
        }

        #endregion

        #region Commands
        
        //NOte - we have to use a command here because x:Bind doesnt work for the item in the item template
        public DelegateCommand<LunchGuest> RemoveLunchToAddGuestCommand
            => removeLunchToAddGuestCommand ??
            (removeLunchToAddGuestCommand = new DelegateCommand<LunchGuest>(async (guest) =>
            {
                await RemoveLunchGuest(guest);
            }));

        #endregion

        #region Methods

        public override async Task<bool> Init()
        {
            try
            {
                LunchAppointments = await LoadLunchAppointments();
                UpdateCurrentGuestsList();

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OverviewViewModel Init Exception: {ex}");
                return false;
            }
        }

        private void UpdateCurrentGuestsList()
        {
            var guests = LunchAppointments.Where(a => a.Guests.Count > 0).SelectMany(a => a.Guests);

            foreach (var lunchGuest in guests)
            {
                if (CurrentLunchGuests.All(g => g.Id != lunchGuest.Id))
                    CurrentLunchGuests.Add(lunchGuest);
            }
        }

        private async Task AddLunchAppointmentAsync()
        {
            try
            {
                await UpdateBusyStatus("adding lunch appointment...");

                if (LunchToAdd == null)
                    return;

                LunchAppointments.Add(LunchToAdd);

                // Hide the overlay
                AddAppointmentOverlayVisibility = Visibility.Collapsed;

                // Save updated appointments list to storage
                await SaveLunchAppointments();

                // Refresh the curent guests list
                UpdateCurrentGuestsList();

                // Reset to clear any partially filled out info
                LunchToAdd = new LunchAppointment();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddLunchAppointmentAsync Exception: {ex}");
            }
            finally
            {
                await UpdateBusyStatus("", false);
            }
        }

        private async Task AddLunchGuestAsync(LunchAppointment appointment)
        {
            try
            {
                await UpdateBusyStatus("adding guest to appointment...");

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

                if (CurrentLunchGuests.Any())
                {
                    // Check to see if they're already in the recent guests list
                    lunchGuest = CurrentLunchGuests.FirstOrDefault(c => c.Id == fullContact.Id);

                    // if the contact is in the recent guests list, use that instance so we dont have to re-save the data
                    if (lunchGuest != null)
                    {
                        Debug.WriteLine("Contact exists in Recent guest list, using that instance");
                        appointment.Guests.Add(lunchGuest);
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

                // Save the contact's thumnail as a jpg image and get the file path for DataBinding to UIElements
                lunchGuest.ThumbnailUri = await SaveThumbnailAsync(fullContact);

                // Now we add an annotation to the contact on the device, 
                // this will allow you to open this app with a protocol directly from the People app
                var annotationStore =
                    await
                        ContactManager.RequestAnnotationStoreAsync(
                            ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);

                var annotationLists = await annotationStore.FindAnnotationListsAsync();

                // See if we already have an annotation list, if not create one
                ContactAnnotationList annotationList;

                if (annotationLists.Count == 0)
                {
                    annotationList = await annotationStore.CreateAnnotationListAsync();
                }
                else
                {
                    annotationList = annotationLists[0];
                }

                // Save this annotation information into the annotation list so that when
                // the People app is opened and that contact is chosen, you'll see this app
                // as an option
                await annotationList.TrySaveAnnotationAsync(new ContactAnnotation
                {
                    ContactId = fullContact.Id,
                    SupportedOperations = ContactAnnotationOperations.Message |
                                          ContactAnnotationOperations.AudioCall |
                                          ContactAnnotationOperations.VideoCall |
                                          ContactAnnotationOperations.ContactProfile
                });


                appointment.Guests.Add(lunchGuest);

                // ************ Since the guest has not been previously added, we need add and save ************ 

                // This is how we keep the list trimmed to just recent guests. If more than 20 exist
                // remove the oldest one
                //if (CurrentLunchGuests.Count > 20)
                //    CurrentLunchGuests.Remove(CurrentLunchGuests.LastOrDefault());

                //// Add the guest to the top of the list
                //CurrentLunchGuests.Insert(0, lunchGuest);

                //// Save the updated list to storage
                //await SaveRecentGuestsAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddLunchGuestAsync Exception: {ex}");
            }
            finally
            {
                await UpdateBusyStatus("", false);
            }
        }

        private async Task RemoveLunchGuest(LunchGuest guest)
        {
            if (guest == null)
                return;

            if (LunchToAdd.Guests.Contains(guest))
            {
                LunchToAdd.Guests.Remove(guest);

                // If the guest is no longer in any appointments, delete the thumbnail image from storage
                foreach (var lunchAppointment in LunchAppointments)
                {
                    if (lunchAppointment.Guests.Contains(guest))
                        await DeleteThumbnailAsync(guest.ThumbnailUri);
                }
            }
        }

        #endregion

        #region x:Bind-able event handlers

        public void AddAppointmentAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            LunchToAdd = new LunchAppointment();

            // If the user pre-selected any recent guests, automatically add them
            if (SelectedCurrentGuests.Count > 0)
            {
                foreach (var selectedRecentGuest in SelectedCurrentGuests)
                {
                    LunchToAdd.Guests.Add(selectedRecentGuest);
                }
            }

            AddAppointmentOverlayVisibility = Visibility.Visible;
        }
        
        public async void SaveLunchToAddButton_OnClick(object sender, RoutedEventArgs e)
        {
            await AddLunchAppointmentAsync();
        }

        public void CancelLunchToAddButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Hide the editor
            AddAppointmentOverlayVisibility = Visibility.Collapsed;

            // Reset to clear any partially filled out info
            LunchToAdd = new LunchAppointment();
        }

        public async void AddLunchGuestButton_OnClick(object sender, RoutedEventArgs e)
        {
            await AddLunchGuestAsync(LunchToAdd);
        }

        public async void RemoveLunchGuestButton_OnClick(object sender, RoutedEventArgs e)
        {
            var guest = ((Button) sender)?.DataContext as LunchGuest;

            if(guest != null)
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

        public void RecentGuestsListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                foreach (var item in e.AddedItems)
                {
                    var guest = item as LunchGuest;

                    if (!SelectedCurrentGuests.Contains(guest))
                        SelectedCurrentGuests.Add(guest);
                }
            }

            if (e.RemovedItems.Count > 0)
            {
                foreach (var item in e.RemovedItems)
                {
                    var guest = item as LunchGuest;

                    if (!SelectedCurrentGuests.Contains(guest))
                        SelectedCurrentGuests.Remove(guest);
                }
            }
        }

        #endregion

        #region Data and file operations

        public async Task SaveLunchAppointments()
        {
            try
            {
                await UpdateBusyStatus("saving LunchAppointments...");

                var json = JsonConvert.SerializeObject(LunchAppointments);

                // UWP Community Toolkit
                await StorageFileHelper.WriteTextToLocalFileAsync(json, Constants.LunchAppointmentsFileName);

                Debug.WriteLine($"--SAVE-- Lunch Appointments JSON:\r\n{json}");
            }
            catch (Exception)
            {
                Debugger.Break();
            }
        }

        public async Task<ObservableCollection<LunchAppointment>> LoadLunchAppointments()
        {
            try
            {
                await UpdateBusyStatus("loading lunch appointments...");

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
                await UpdateBusyStatus("", false);
            }
        }
        
        /// <summary>
        /// Save the Contact's thumbnail as a jpg in the app's storage
        /// </summary>
        /// <param name="contact">The Contact to save the thumbnail for</param>
        /// <returns>Complete file path of jpg image</returns>
        private static async Task<string> SaveThumbnailAsync(Contact contact)
        {
            try
            {
                if (contact.Thumbnail == null)
                    return "";

                using (var stream = await contact.Thumbnail.OpenReadAsync())
                {
                    byte[] buffer = new byte[stream.Size];
                    var readBuffer =
                        await stream.ReadAsync(buffer.AsBuffer(), (uint) buffer.Length, InputStreamOptions.None);

                    var file = await ApplicationData.Current.LocalFolder.CreateFileAsync($"{contact.FullName}_Thumb.jpg",
                                CreationCollisionOption.ReplaceExisting);

                    using (var fileStream = await file.OpenStreamForWriteAsync())
                    {
                        await fileStream.WriteAsync(readBuffer.ToArray(), 0, (int) readBuffer.Length);
                    }

                    Debug.WriteLine($"Thumbnail saved - Path: {file.Path}");

                    // Return the saved image's file path
                    return file.Path;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveThumbnailAsync Exception: {ex}");
                return "";
            }
        }

        private static async Task DeleteThumbnailAsync(string filePath)
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(filePath);

                if (file != null)
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception)
            {
                    
                throw;
            }
        }

        #endregion
    }
}
