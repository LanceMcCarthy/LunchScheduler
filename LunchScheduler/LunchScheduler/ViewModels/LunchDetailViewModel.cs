using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Common;
using LunchScheduler.Data.Models;
using LunchScheduler.Helpers;
using Microsoft.Toolkit.Uwp.Services.Facebook;
using Microsoft.Toolkit.Uwp.Services.Twitter;
using Template10.Mvvm;

namespace LunchScheduler.ViewModels
{
    public class LunchDetailViewModel : ViewModelBase
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

        #region Properties

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

        #endregion
        
        #region Methods

        public override Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            try
            {
                UpdateStatus("loading...");

                var appointment = parameter as LunchAppointment;

                if (appointment != null)
                {
                    SelectedAppointment = appointment;
                }

                return base.OnNavigatedToAsync(parameter, mode, state);

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatedToAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("", false);
            }

            return base.OnNavigatedToAsync(parameter, mode, state);
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
                foreach (var guest in SelectedAppointment.Guests)
                {
                    var result = appointments.Where(a => a.Guests.Any(g => g == guest));

                    if (!result.Any())
                    {
                        UpdateStatus($"deleting {guest.FullName}'s photo...");
                        await FileHelpers.DeleteThumbnailAsync(guest.ThumbnailUri);
                    }
                }

                // Now we can remove the appointment
                appointments.Remove(SelectedAppointment);

                // Save the updated list
                await FileHelpers.SaveLunchAppointments(appointments);

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
