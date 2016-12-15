using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Common;
using LunchScheduler.ViewModels;
using LunchScheduler.Views;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;

namespace LunchScheduler
{
    sealed partial class App : Application
    {
        // Expose a reference to the content frame so that we can navigate from any of the ViewModels
        public static Frame ContentFrame { get; private set; }

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
        
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            // Opt out of Prelaunch
            if (e.PrelaunchActivated)
                return;

            var shell = Window.Current.Content as ShellPage;

            if (shell == null)
            {
                // Create a Shell which navigates to the first page
                shell = new ShellPage();

                // hook-up shell root frame navigation events
                shell.ContentFrame.NavigationFailed += Shell_OnNavigationFailed;
                shell.ContentFrame.Navigated += Shell_OnNavigated;
                ContentFrame = shell.ContentFrame;

                await RestoreStatusAsync(e.PreviousExecutionState);

                // set the Shell as content
                Window.Current.Content = shell;

                // Hook into back button press (this works for both hardware and software back button)
                SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

                UpdateBackButtonVisibility();
            }

            Window.Current.Activate();
        }

        protected override async void OnActivated(IActivatedEventArgs e)
        {
            try
            {
                base.OnActivated(e);
                
                var shell = Window.Current.Content as ShellPage;

                if (shell == null)
                {
                    shell = new ShellPage();
                    shell.ContentFrame.NavigationFailed += Shell_OnNavigationFailed;
                    shell.ContentFrame.Navigated += Shell_OnNavigated;
                    ContentFrame = shell.ContentFrame;

                    await RestoreStatusAsync(e.PreviousExecutionState);

                    // set the Shell as content
                    Window.Current.Content = shell;

                    // Hook into back button press (this works for both hardware and software back button)
                    SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
                    
                    UpdateBackButtonVisibility();
                }
                
                #region Toast activation

                if (e.Kind == ActivationKind.ToastNotification)
                {
                    var toastArgs = e as ToastNotificationActivatedEventArgs;
                    var argument = toastArgs?.Argument;

                    Debug.WriteLine($"OnActivated ToastNotification argument: {argument}");

                    if (argument != null && argument.Contains("Id"))
                    {
                        shell.ContentFrame.Navigate(typeof(LunchDetailPage), argument);
                    }
                }

                #endregion
                
                Window.Current.Activate();
            }
            catch (Exception ex)
            {
                await new MessageDialog($"App.OnActivated Exception: {ex}").ShowAsync();
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

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (ContentFrame.CanGoBack)
            {
                e.Handled = true;
                ContentFrame.GoBack();
            }
        }

        private void Shell_OnNavigated(object sender, NavigationEventArgs e)
        {
            UpdateBackButtonVisibility();
        }

        private void UpdateBackButtonVisibility()
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = ContentFrame.CanGoBack 
                ? AppViewBackButtonVisibility.Visible 
                : AppViewBackButtonVisibility.Collapsed;
        }
        
        private void Shell_OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
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

                var file = ApplicationData.Current.TemporaryFolder.TryGetItemAsync(Constants.ProfileViewModelFileName);

                if (file == null)
                {
                    await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Constants.ProfileViewModelFileName, CreationCollisionOption.ReplaceExisting);
                }

                // UWP Community Toolkit
                await StorageFileHelper.WriteTextToLocalFileAsync(json, Constants.ProfileViewModelFileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CacheViewModelException: {ex}");
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
