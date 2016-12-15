using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Models;
using LunchScheduler.ViewModels;

namespace LunchScheduler.Views
{
    public sealed partial class LunchDetailPage : Page
    {
        public LunchDetailPage()
        {
            this.InitializeComponent();
        }

        // If a parameter is passed, use it for the details
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var appointment = e?.Parameter as LunchAppointment;

            if (appointment != null && DataContext is LunchDetailViewModel)
            {
                ((LunchDetailViewModel) DataContext).SelectedAppointment = appointment;
            }
        }
    }
}
