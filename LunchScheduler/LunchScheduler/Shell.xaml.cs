using Windows.UI.Xaml.Controls;
using Template10.Controls;
using Template10.Services.NavigationService;

namespace LunchScheduler
{
    public sealed partial class Shell : Page
    {
        public Shell(INavigationService navigationService) : this()
        {
            this.InitializeComponent();
            Menu.NavigationService = navigationService;
        }

        public static Shell Instance { get; set; }
        public static HamburgerMenu HamburgerMenu => Instance.Menu;

        public Shell()
        {
            Instance = this;
            InitializeComponent();
        }
        
    }
}
