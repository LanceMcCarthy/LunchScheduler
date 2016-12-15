using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Models;

namespace LunchScheduler.Views
{
    public sealed partial class GuestDetailPage : Page
    {
        public LunchGuest Guest { get; set; }

        public GuestDetailPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e?.Parameter != null)
            {
                this.DataContext = e.Parameter as LunchGuest;
            }
        }
    }
}
