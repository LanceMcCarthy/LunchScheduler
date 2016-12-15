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
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
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
        //private DelegateCommand addContactCommand;

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

        //public DelegateCommand AddContactCommand => addContactCommand ?? (addContactCommand = new DelegateCommand(async () => await AddContactAsync()));
        
        #endregion



        #region Methods

        public override async Task<bool> Init()
        {
            try
            {
                //CurrentGuests = await LoadCurrentGuests();
                
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

        public void CallSelectedContact_OnClick(object sender, RoutedEventArgs e)
        {
            //throw new System.NotImplementedException();
        }

        public void EmailSelectedContact_OnClick(object sender, RoutedEventArgs e)
        {
            //throw new System.NotImplementedException();
        }

        //public async Task SaveCurrentGuests()
        //{
        //    try
        //    {
        //        await UpdateBusyStatus("saving currentGuests...");

        //        var json = JsonConvert.SerializeObject(CurrentGuests);

        //        // UWP Community Toolkit
        //        await StorageFileHelper.WriteTextToLocalFileAsync(json, Constants.RecentLunchGuestsFileName);

        //        Debug.WriteLine($"--SaveCurrentGuests--\r\n{json}");
        //    }
        //    catch (Exception)
        //    {
        //        Debugger.Break();
        //    }
        //}

        //public async Task<ObservableCollection<LunchGuest>> LoadCurrentGuests()
        //{
        //    try
        //    {
        //        await UpdateBusyStatus("loading currentGuests...");

        //        // UWP Community Toolkit
        //        var json = await StorageFileHelper.ReadTextFromLocalFileAsync(Constants.RecentLunchGuestsFileName);

        //        Debug.WriteLine($"--LoadCurrentGuests--\r\n{json}");

        //        var storedContacts = JsonConvert.DeserializeObject<ObservableCollection<LunchGuest>>(json);

        //        return storedContacts;
        //    }
        //    catch (FileNotFoundException fnfException)
        //    {
        //        Debug.WriteLine($"LoadCurrentGuests FileNotFoundException: {fnfException}");
        //        return new ObservableCollection<LunchGuest>();
        //        //await StorageFileHelper.WriteTextToLocalFileAsync("", Constants.RecentLunchGuestsFileName);
        //        //await LoadCurrentGuests();
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"LoadCurrentGuests Exception: {ex}");
        //        return new ObservableCollection<LunchGuest>();
        //    }
        //    finally
        //    {
        //        await UpdateBusyStatus("", false);
        //    }
        //}

        //private async Task AddContactAsync()
        //{
        //    try
        //    {
        //        await UpdateBusyStatus("adding contact...");

        //        // User selects a contact from their device
        //        var userSelectedContact = await new ContactPicker().PickContactAsync();

        //        // We need to get the full contact info so we have access to the thumbnail
        //        var contactStore = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AllContactsReadOnly);
        //        var fullContact = await contactStore.GetContactAsync(userSelectedContact.Id);

        //        if (fullContact == null)
        //            return;

        //        var isUpdatingExistingContact = false;

        //        var lunchContact = CurrentGuests.FirstOrDefault(c => c.Id == fullContact.Id);

        //        // If we have already got the lunch contact in the app, as user if they intend to update the details
        //        if (lunchContact != null)
        //        {
        //            // Note: This approach awaits the task result
        //            isUpdatingExistingContact = await CoreWindow.GetForCurrentThread().Dispatcher.RunTaskAsync(async () =>
        //            {
        //                var popup = new MessageDialog($"You already have {fullContact.FullName} in your Lunch currentGuests list. Do you want to update your existing lunch contact?", "Update Info?");
        //                popup.Commands.Add(new UICommand("yes"));
        //                popup.Commands.Add(new UICommand("no"));
        //                popup.CancelCommandIndex = 1;
        //                var command = await popup.ShowAsync();

        //                return command.Label == "yes";
        //            });

        //            // If the user doesn't want to update the contact info, return.
        //            if (!isUpdatingExistingContact)
        //                return;

        //            Debug.WriteLine("Contact exists, updating info");
        //        }

        //        // We want to copy out the relevant information into our model
        //        lunchContact = new LunchGuest
        //        {
        //            Id = fullContact.Id,
        //            FullName = fullContact.FullName
        //        };

        //        // Add each phone number
        //        foreach (var contactPhone in fullContact.Phones)
        //        {
        //            lunchContact.PhoneNumbers.Add(new LunchGuestPhoneNumber
        //            {
        //                Description = contactPhone.Kind.ToString("G"),
        //                Number = contactPhone.Number
        //            });
        //        }

        //        // Add each email address
        //        foreach (var contactEmail in fullContact.Emails)
        //        {
        //            lunchContact.EmailAddresses.Add(new LunchGuestEmailAddress
        //            {
        //                Description = contactEmail.Kind.ToString("G"),
        //                Address = contactEmail.Address
        //            });
        //        }

        //        lunchContact.ThumbnailUri = await SaveThumbnailAsync(fullContact);

        //        // Now we add an annotation to the contact on the dcevice, 
        //        // this will allow you to open this app with a protocol directly from the People app
        //        var annotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);

        //        var annotationLists = await annotationStore.FindAnnotationListsAsync();

        //        // See if we already have an annotation list, if not create one
        //        ContactAnnotationList annotationList;

        //        if (annotationLists.Count == 0)
        //        {
        //            annotationList = await annotationStore.CreateAnnotationListAsync();
        //        }
        //        else
        //        {
        //            annotationList = annotationLists[0];
        //        }

        //        // Save this annotation information into the annotation list so that when
        //        // the People app is opened and that contact is chosen, you'll see this app
        //        // as an option
        //        await annotationList.TrySaveAnnotationAsync(new ContactAnnotation
        //        {
        //            ContactId = fullContact.Id,
        //            SupportedOperations = ContactAnnotationOperations.Message |
        //                                  ContactAnnotationOperations.AudioCall |
        //                                  ContactAnnotationOperations.VideoCall |
        //                                  ContactAnnotationOperations.ContactProfile
        //        });

        //        if (!isUpdatingExistingContact)
        //        {
        //            // Finally add the contact to the app's lunch currentGuests list if they're not already present
        //            CurrentGuests.Add(lunchContact);
        //        }

        //        // Save updated currentGuests list to storage
        //        await SaveCurrentGuests();
        //    }
        //    catch (Exception)
        //    {
        //        Debugger.Break();
        //    }
        //    finally
        //    {
        //        await UpdateBusyStatus("", false);
        //    }
        //}

        //private static async Task<string> SaveThumbnailAsync(Contact contact)
        //{
        //    try
        //    {
        //        if (contact.Thumbnail == null)
        //            return "";

        //        using (var stream = await contact.Thumbnail.OpenReadAsync())
        //        {
        //            byte[] buffer = new byte[stream.Size];
        //            var readBuffer = await stream.ReadAsync(buffer.AsBuffer(), (uint)buffer.Length, InputStreamOptions.None);

        //            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync($"{contact.FullName}_Thumb.jpg", CreationCollisionOption.ReplaceExisting);
        //            using (var fileStream = await file.OpenStreamForWriteAsync())
        //            {
        //                await fileStream.WriteAsync(readBuffer.ToArray(), 0, (int)readBuffer.Length);
        //            }

        //            Debug.WriteLine($"Thumbnail saved - Path: {file.Path}");

        //            // Return the saved image's file path
        //            return file.Path;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debugger.Break();
        //        return "";
        //    }
        //}

        #endregion
    }
}
