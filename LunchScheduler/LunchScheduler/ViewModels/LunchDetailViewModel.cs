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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Common;
using LunchScheduler.Data.Models;
using LunchScheduler.Helpers;
using Microsoft.Toolkit.Uwp.Services.Facebook;
using Microsoft.Toolkit.Uwp.Services.Twitter;
using Template10.Common;
using Template10.Mvvm;

namespace LunchScheduler.ViewModels
{
    public class LunchDetailViewModel : ViewModelBase
    {
        private const string FacebookProfilePhotoUrlSettingsKey = "FacebookProfilePhotoUrl";
        private const string TwitterProfilePhotoUrlSettingsKey = "TwitterProfilePhotoUrl";
        private readonly ApplicationDataContainer localSettings;
        private LunchAppointment selectedAppointment;
        private bool isBusy;
        private string isBusyMessage;
        private string facebookProfilePictureUrl;
        private string twitterProfilePictureUrl;

        public LunchDetailViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                SelectedAppointment = DesignTimeHelpers.GenerateSampleAppointment();
                return;
            }

            localSettings = ApplicationData.Current.LocalSettings;
        }

        #region Properties

        public LunchAppointment SelectedAppointment
        {
            get { return selectedAppointment; }
            set { Set(ref selectedAppointment, value); }
        }
        
        public string FacebookProfilePictureUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(facebookProfilePictureUrl))
                    return facebookProfilePictureUrl;

                object obj;
                if (localSettings != null && localSettings.Values.TryGetValue(FacebookProfilePhotoUrlSettingsKey, out obj))
                {
                    facebookProfilePictureUrl = obj.ToString();
                }

                return facebookProfilePictureUrl;
            }
            set
            {
                Set(ref facebookProfilePictureUrl, value);

                if (localSettings != null)
                    localSettings.Values[FacebookProfilePhotoUrlSettingsKey] = value;
            }
        }

        public string TwitterProfilePictureUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(twitterProfilePictureUrl))
                    return twitterProfilePictureUrl;

                object obj;
                if (localSettings != null && localSettings.Values.TryGetValue(TwitterProfilePhotoUrlSettingsKey, out obj))
                {
                    twitterProfilePictureUrl = obj.ToString();
                }

                return twitterProfilePictureUrl;
            }
            set
            {
                Set(ref twitterProfilePictureUrl, value);

                if (localSettings != null)
                    localSettings.Values[TwitterProfilePhotoUrlSettingsKey] = value;
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

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            try
            {
                UpdateStatus("loading...");

                var appointment = parameter as LunchAppointment;

                if (appointment != null)
                {
                    SelectedAppointment = appointment;
                }

                // If we've stored profile picture Urls, the user has previously authenticated
                if (!string.IsNullOrEmpty(FacebookProfilePictureUrl))
                {
                    await LogIntoFacebookAsync();
                }

                if (!string.IsNullOrEmpty(TwitterProfilePictureUrl))
                {
                    await LogIntoTwitterAsync();
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

            //return base.OnNavigatedToAsync(parameter, mode, state);
        }

        /// <summary>
        /// Creates a message using the lunch title, guests and location
        /// </summary>
        /// <returns></returns>
        private string ConstructMessage()
        {
            string message = $"Having lunch at {SelectedAppointment.LunchTime:t} with";

            if (SelectedAppointment.Guests.Count > 0)
            {
                for (int i = 0; i < SelectedAppointment.Guests.Count - 1; i++)
                {
                    // Add the proper punctuation and grammar between guest names

                    if (i == 0)
                        message += ": "; // First guest
                    else if (i == SelectedAppointment.Guests.Count - 1)
                        message += " and "; // Last guest
                    else
                        message += ", ";

                    // Add the guest's name
                    message += $"{SelectedAppointment.Guests[i].FullName}";
                }
            }

            if (SelectedAppointment.Location != null)
            {
                message += $" at {SelectedAppointment.Location.Name}";
            }

            return message;
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

        private async Task<bool> LogIntoFacebookAsync()
        {
            try
            {
                UpdateStatus("logging into Facebook...");

                FacebookService.Instance.Initialize(ServiceKeys.FacebookAppId);

                if (await FacebookService.Instance.LoginAsync())
                {
                    var photo = await FacebookService.Instance.GetUserPictureInfoAsync();

                    FacebookProfilePictureUrl = photo.Url;

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LogIntoFacebookAsync Exception: {ex}");
                return false;
            }
            finally
            {
                UpdateStatus("");
            }
        }
        public async void LogOutOfFacebook(object sender, RoutedEventArgs args)
        {
            await FacebookService.Instance.LogoutAsync();
            
            FacebookProfilePictureUrl = "";
        }

        private async Task<bool> LogIntoTwitterAsync()
        {
            try
            {
                UpdateStatus("logging into Twitter...");

                TwitterService.Instance.Initialize(
                    ServiceKeys.TwitterConsumerKey,
                    ServiceKeys.TwitterConsumerSecret,
                    ServiceKeys.CallbackUri);

                if (await TwitterService.Instance.LoginAsync())
                {
                    var user = await TwitterService.Instance.GetUserAsync();
                    TwitterProfilePictureUrl = user.ProfileImageUrl;

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LogIntoTwitterAsync Exception: {ex}");
                return false;
            }
            finally
            {
                UpdateStatus("");
            }
        }

        public void LogOutOfTwitter(object sender, RoutedEventArgs args)
        {
            TwitterService.Instance.Logout();

            TwitterProfilePictureUrl = "";
        }
        
        #endregion

        #region Event handlers

        public async void DeleteAppointmentButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedAppointment == null)
                return;
            
            try
            {
                UpdateStatus("deleting appointment...");

                var md = new MessageDialog("Are you sure you want to delete?", "Delete Lunch");
                md.Commands.Add(new UICommand("delete"));
                md.Commands.Add(new UICommand("cancel"));
                var dialogResult = await md.ShowAsync();

                if (dialogResult.Label == "cancel")
                    return;

                UpdateStatus("loading all appointments...");

                var appointments = await FileHelpers.LoadLunchAppointments();

                // First let's clean up any guest photo files if they're not in any other appointments
                foreach (var guest in selectedAppointment.Guests)
                {
                    var result = appointments.Where(a => a.Guests.Any(g => g.Id == guest.Id));

                    if (!result.Any())
                    {
                        UpdateStatus($"deleting {guest.FullName}'s photo...");
                        await FileHelpers.DeleteThumbnailAsync(guest.ThumbnailUri);
                    }
                }

                // Now we can remove the appointment
                appointments.Remove(selectedAppointment);

                // Save the updated list
                await FileHelpers.SaveLunchAppointments(appointments);

               if (BootStrapper.Current.NavigationService.CanGoBack)
                    BootStrapper.Current.NavigationService.GoBack();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteLunchAppointmentAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("");
            }
        }

        public async void EmailGuestsButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedAppointment.Guests == null || SelectedAppointment.Guests.Count == 0)
                return;

            var emailMessage = new EmailMessage();

            foreach (var guest in SelectedAppointment.Guests)
            {
                var email = guest.EmailAddresses.FirstOrDefault();

                if (email != null)
                {
                    var emailRecipient = new EmailRecipient(email.Address);
                    emailMessage.Bcc.Add(emailRecipient);
                }
            }

            emailMessage.Subject = "Let's eat!";

            emailMessage.Body = $"Let's have a bite to eat on {SelectedAppointment.LunchTime:f} at {SelectedAppointment.Location.Name}.\r\n\nThe address is {SelectedAppointment.Location.Address}";

            await EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }

        public async void FacebookShareButton_OnClick(object sender, RoutedEventArgs e)
        {
            var success = false;

            try
            {
                if (SelectedAppointment == null)
                    return;

                var message = ConstructMessage();
                
                if (await LogIntoFacebookAsync())
                {
                    // Facebook share requires a URL or an image
                    success = await FacebookService.Instance.PostToFeedAsync(
                        "TEST", 
                        message, 
                        "Lunch Scheduler demo",
                        "https://raw.githubusercontent.com/LanceMcCarthy/LunchScheduler/master/LunchScheduler/Screenshots/GuestDetailPage.png");
                }
                else
                {
                    await new MessageDialog("Make sure you've added your API keys to ServiceKeys.cs!","You were not signed into Facebook").ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FacebookShareButton_OnClick Exception: {ex}");
            }
            finally
            {
                if (success)
                    await new MessageDialog("Your post was successful!").ShowAsync();
            }
        }

        public async void TwitterShareButton_OnClick(object sender, RoutedEventArgs e)
        {
            var success = false;

            try
            {
                if (SelectedAppointment == null)
                    return;

                var message = ConstructMessage();
                
                if (await LogIntoTwitterAsync())
                {
                    success = await TwitterService.Instance.TweetStatusAsync("TEST: " + message);
                }
                else
                {
                    await new MessageDialog("Make sure you've added your API keys to ServiceKeys.cs!", "You were not signed into Twitter").ShowAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TwitterShareButton_OnClick Exception: {ex}");
            }
            finally
            {
                if (success)
                    await new MessageDialog("Your post was successful!").ShowAsync();
            }
        }

        #endregion
    }
}
