using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Services.Facebook;
using Microsoft.Toolkit.Uwp.Services.Twitter;
using Newtonsoft.Json;

namespace LunchScheduler.ViewModels
{
    public class LunchDetailViewModel : Template10.Mvvm.ViewModelBase
    {
        private LunchAppointment selectedAppointment;
        private bool isBusy;
        private string isBusyMessage;

        public LunchDetailViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                SelectedAppointment = DesignTimeHelpers.GenerateSampleAppointment();
            }
        }

        public LunchAppointment SelectedAppointment
        {
            get { return selectedAppointment; }
            set { Set(ref selectedAppointment, value); }
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

        public override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            var appointment = parameter as LunchAppointment;

            if (appointment != null)
            {
                SelectedAppointment = appointment;
            }

            return base.OnNavigatedToAsync(parameter, mode, state);
        }

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
        /// Shows busy indicator
        /// </summary>
        /// <param name="message">busy message to show</param>
        /// <param name="showBusyIndicator">toggles the busy indicator's visibility</param>
        private void UpdateStatus(string message, bool showBusyIndicator = true)
        {
            IsBusy = showBusyIndicator;
            IsBusyMessage = message;
        }
        
        /// <summary>
        /// Loads saved lunch appointments from file
        /// </summary>
        /// <returns>ObservableCollection of LunchAppointments</returns>
        private async Task<ObservableCollection<LunchAppointment>> LoadLunchAppointments()
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
        /// Saves lunch appointments to storage
        /// </summary>
        /// <returns></returns>
        private async Task SaveLunchAppointments(ObservableCollection<LunchAppointment> appointments)
        {
            try
            {
                UpdateStatus("saving appointments...");

                var json = JsonConvert.SerializeObject(appointments);

                // UWP Community Toolkit
                await StorageFileHelper.WriteTextToLocalFileAsync(json, Constants.LunchAppointmentsFileName);

                Debug.WriteLine($"--SAVE-- Lunch Appointments JSON:\r\n{json}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveLunchAppointments Exception: {ex}");
            }
        }
        
        /// <summary>
        /// Checks to see if there is a thumbnail photo and deletes it
        /// </summary>
        /// <param name="filePath">File path to lunch guest's photo</param>
        /// <returns></returns>
        private async Task DeleteThumbnailAsync(string filePath)
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(filePath);

                if (file != null)
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteThumbnailAsync Exception: {ex}");
            }
        }

        #region Event handlers

        public async void DeleteAppointmentButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SelectedAppointment == null)
                return;

            var md = new MessageDialog("Are you sure you want to delete?", "Delete Lunch");
            md.Commands.Add(new UICommand("delete"));
            md.Commands.Add(new UICommand("cancel"));
            var dialogResult = await md.ShowAsync();

            if (dialogResult.Label == "cancel")
                return;

            try
            {
                UpdateStatus("deleting appointment...");

                var appointments = await LoadLunchAppointments();

                // First let's clean up any guest photo files if they're not in any other appointments
                foreach (var guest in SelectedAppointment.Guests)
                {
                    var result = appointments.Where(a => a.Guests.Any(g => g == guest));

                    if (!result.Any())
                        await DeleteThumbnailAsync(guest.ThumbnailUri);
                }

                // Now we can remove the appointment
                appointments.Remove(SelectedAppointment);

                // Save the updated list
                await SaveLunchAppointments(appointments);

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteLunchAppointmentAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("", false);
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

                // UWP Toolkit
                FacebookService.Instance.Initialize(ServiceKeys.FacebookAppId);

                if (!await FacebookService.Instance.LoginAsync())
                {
                    await new MessageDialog("You were not signed into Facebook").ShowAsync();
                    return;
                }

                // Facebook share requires a URL or an image
                success = await FacebookService.Instance.PostToFeedAsync("TEST", message, "Lunch Scheduler demo", "https://github.com/Microsoft/Windows-universal-samples/blob/master/SharedContent/media/splash-sdk.png");

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

                // UWP Toolkit
                TwitterService.Instance.Initialize(ServiceKeys.TwitterConsumerKey,
                    ServiceKeys.TwitterConsumerSecret,
                    ServiceKeys.CallbackUri);

                if (!await TwitterService.Instance.LoginAsync())
                {
                    await new MessageDialog("You were not signed into Twitter").ShowAsync();
                    return;
                }

                success = await TwitterService.Instance.TweetStatusAsync("TEST: " + message);

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
