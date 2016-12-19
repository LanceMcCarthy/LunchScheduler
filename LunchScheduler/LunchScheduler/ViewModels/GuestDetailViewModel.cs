using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Calls;
using Windows.ApplicationModel.Email;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Common;
using LunchScheduler.Data.Models;
using LunchScheduler.Helpers;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
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


                // Get all the appointments the guest is invited to
                allAppointments = await LoadLunchAppointments();

                var invitedTo = allAppointments.Where(a => a.Guests.Contains(guest));

                foreach (var lunch in invitedTo)
                {
                    InvitedAppointments.Add(lunch);
                }
            }
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
        /// Checks to see if there is a thumbnail photo and deletes it
        /// </summary>
        /// <param name="filePath">File path to lunch guest's photo</param>
        /// <returns></returns>
        private async Task DeleteThumbnailAsync(string filePath)
        {
            try
            {
                UpdateStatus("deleting thumbnail");

                var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(filePath);

                if (file != null)
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteThumbnailAsync Exception: {ex}");
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
                var md = new MessageDialog("Deleting this guest will also remove them from all lunches. Are you sure you want to delete?", "Delete Guest?");
                md.Commands.Add(new UICommand("delete"));
                md.Commands.Add(new UICommand("cancel"));
                var dialogResult = await md.ShowAsync();

                if (dialogResult.Label == "cancel")
                    return;

                // If the guest isnt in any other appointments, remove the thumbnail from storage
                var result = allAppointments.Where(a => a.Guests.Any(g => g == SelectedGuest));

                if (!result.Any())
                    await DeleteThumbnailAsync(SelectedGuest.ThumbnailUri);

                if(BootStrapper.Current.NavigationService.CanGoBack)
                    BootStrapper.Current.NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteContact_OnClick Exception: {ex}");
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
