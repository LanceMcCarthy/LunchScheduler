using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;
using Windows.UI.Xaml;
using LunchScheduler.Data.Models;
using LunchScheduler.Helpers;

namespace LunchScheduler.ViewModels
{
    public class LunchDetailViewModel : ViewModelBase
    {
        private LunchAppointment selectedAppointment;

        public LunchDetailViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                SelectedAppointment = DesignTimeHelpers.GenerateSampleAppointment();
            }
        }

        public LunchAppointment SelectedAppointment
        {
            get { return selectedAppointment ?? (selectedAppointment = new LunchAppointment()); }
            set { SetProperty(ref selectedAppointment, value); }
        }

        public override async Task<bool> Init()
        {
            return true;
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
    }
}
