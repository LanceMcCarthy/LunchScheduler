using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using LunchScheduler.Data.Common;
using LunchScheduler.ViewModels;
using LunchScheduler.Views;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using Template10.Common;
using Template10.Controls;

namespace LunchScheduler
{
    sealed partial class App : BootStrapper
    {
        // Expose a reference to the content frame so that we can navigate from any of the ViewModels
        public static Shell Shell { get; private set; }

        // This ViewModel is for the user's chosen social login/profile
        private static ProfileViewModel profileViewModel;
        public static ProfileViewModel ProfileViewModel
        {
            get { return profileViewModel ?? (profileViewModel = new ProfileViewModel()); }
            private set { profileViewModel = value; }
        }
        
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
        }

        public override UIElement CreateRootElement(IActivatedEventArgs e)
        {
            var service = NavigationServiceFactory(BackButton.Attach, ExistingContent.Exclude);

            Shell = new Shell(service);

            return new ModalDialog
            {
                DisableBackButtonWhenModal = true,
                Content = Shell
            };
        }

        public override async Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
           await RestoreStatusAsync(args.PreviousExecutionState);

            if (args.Kind == ActivationKind.ToastNotification)
            {
                var toastArgs = args as ToastNotificationActivatedEventArgs;
                var argument = toastArgs?.Argument;

                if (argument != null && argument.Contains("id"))
                {
                    Debug.WriteLine($"OnActivated ToastNotification argument: {argument}");

                    NavigationService.Navigate(typeof(OverviewPage), argument);
                }
            }
            else
            {
                NavigationService.Navigate(typeof(OverviewPage));
            }
        }
        
        private async Task RestoreStatusAsync(ApplicationExecutionState previousExecutionState)
        {
            try
            {
                Debug.WriteLine("RESTORE STATUS from " + previousExecutionState);

                switch (previousExecutionState)
                {
                    case ApplicationExecutionState.Terminated:
                        ProfileViewModel = await GetCachedViewModelAsync();
                        break;
                    case ApplicationExecutionState.Running:
                        break;
                    case ApplicationExecutionState.NotRunning:
                        break;
                    case ApplicationExecutionState.ClosedByUser:
                        break;
                    case ApplicationExecutionState.Suspended:
                        break;
                }
            }
            catch (Exception ex)
            {
                await new MessageDialog($"App.RestoreStatusAsync Exception: {ex}").ShowAsync();
            }
        }
        
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();

            await CacheViewModelAsync();

            deferral.Complete();
        }

        public static string Serialize(object o)
        {
            Debug.WriteLine("---App.Serialize---");

            var json = JsonConvert.SerializeObject(o);
            return json;
        }

        public static T Deserialize<T>(string json)
        {
            Debug.WriteLine($"---App.Deserialize<{typeof(T)}>---");
            
            var instance = JsonConvert.DeserializeObject<T>(json);
            return instance;
        }

        private static async Task CacheViewModelAsync()
        {
            try
            {
                var json = Serialize(ProfileViewModel);

                // Check to see if file is there
                var file = ApplicationData.Current.TemporaryFolder.TryGetItemAsync(Constants.ProfileViewModelFileName);
                if (file == null)
                    await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Constants.ProfileViewModelFileName, CreationCollisionOption.ReplaceExisting);

                // Then save with UWP Community Toolkit helper
                await StorageFileHelper.WriteTextToLocalFileAsync(json, Constants.ProfileViewModelFileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CachedViewModelException: {ex}");
            }
        }

        private static async Task<ProfileViewModel> GetCachedViewModelAsync()
        {
            try
            {
                var json = await StorageFileHelper.ReadTextFromLocalFileAsync(Constants.ProfileViewModelFileName);
                var persistedViewModel = Deserialize<ProfileViewModel>(json);

                if (persistedViewModel == null)
                {
                    return new ProfileViewModel();
                }

                return persistedViewModel;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GetCachedViewModelAsync: {ex}");
                return new ProfileViewModel();
            }
        }
    }
}
