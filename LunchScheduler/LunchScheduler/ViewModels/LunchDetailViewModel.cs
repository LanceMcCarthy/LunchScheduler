using System.Threading.Tasks;
using Windows.ApplicationModel;
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

        public LunchDetailViewModel(LunchAppointment appointment)
        {
            SelectedAppointment = appointment;
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
    }
}
