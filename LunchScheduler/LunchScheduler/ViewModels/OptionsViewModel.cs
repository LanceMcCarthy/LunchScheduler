//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.BackgroundTasks;
using LunchScheduler.Helpers;
using Template10.Mvvm;

namespace LunchScheduler.ViewModels
{
    public class OptionsViewModel : ViewModelBase
    {
        private readonly ApplicationDataContainer localSettings;
        private const string MonitorLunchesTaskFriendlyName = "LunchMonitorTask";
        private bool isBackgroundTaskEnabled;
        private string lastTaskStatusMessage;
        private string currentStatus = "Reminders Background Task Disabled";
        private SolidColorBrush currentStatusBrush = new SolidColorBrush(Colors.Red);
        private List<int> monitorTimeWindows;
        private int selectedMonitorTimeWindow = 30;
        private bool isBusy;
        private string isBusyMessage;

        public OptionsViewModel()
        {
            if (DesignMode.DesignModeEnabled)
                return;

            localSettings = ApplicationData.Current.LocalSettings;
        }

        #region Properties

        public bool IsBackgroundTaskEnabled
        {
            get { return isBackgroundTaskEnabled; }
            set
            {
                Set(ref isBackgroundTaskEnabled, value);

                // Enable or disable the task accordingly
                if (value)
                    EnableBackgroundTask();
                else
                    DisableBackgroundTask();
            }
        }

        public string CurrentStatus
        {
            get { return currentStatus; }
            set { Set(ref currentStatus, value); }
        }

        public SolidColorBrush CurrentStatusBrush
        {
            get { return currentStatusBrush; }
            set { Set(ref currentStatusBrush, value); }
        }

        public string LastTaskStatusMessage
        {
            get
            {
                if (DesignMode.DesignModeEnabled)
                    return lastTaskStatusMessage;

                object obj;
                if (localSettings != null && localSettings.Values.TryGetValue("BgTaskStatus", out obj))
                {
                    lastTaskStatusMessage = obj.ToString();
                }

                return lastTaskStatusMessage;
            }
            set { Set(ref lastTaskStatusMessage, value); }
        }

        public List<int> MonitorTimeWindows => monitorTimeWindows ?? (monitorTimeWindows = new List<int> { 15, 30, 60, 120 });

        /// <summary>
        /// This property sets the amount of time ahead to check for any appointments.
        /// </summary>
        public int SelectedMonitorTimeWindow
        {
            get
            {
                if (DesignMode.DesignModeEnabled)
                    return selectedMonitorTimeWindow;

                object obj;
                if (localSettings != null && localSettings.Values.TryGetValue("SelectedMonitorTimeWindow", out obj))
                {
                    selectedMonitorTimeWindow = (int)obj;
                }

                return selectedMonitorTimeWindow;
            }
            set
            {
                Set(ref selectedMonitorTimeWindow, value);

                if (localSettings != null)
                    localSettings.Values["SelectedMonitorTimeWindow"] = selectedMonitorTimeWindow;

                EnableBackgroundTask(selectedMonitorTimeWindow);
            }
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

        #region Methods and Event handlers

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            try
            {
                UpdateStatus("loading...");

                IsBackgroundTaskEnabled = await BackgroundTaskHelpers.CheckBackgroundTasksAsync(MonitorLunchesTaskFriendlyName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatedToAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("", false);
            }
        }

        private async void EnableBackgroundTask(int frequency = 30)
        {
            try
            {
                UpdateStatus("registering background task...");

                var accessStatus = await BackgroundExecutionManager.RequestAccessAsync();

                switch (accessStatus)
                {
                    // If we are allowed to register a background task
                    case BackgroundAccessStatus.AlwaysAllowed:
                    case BackgroundAccessStatus.AllowedSubjectToSystemPolicy:
                        BackgroundTaskHelpers.RegisterAsync(MonitorLunchesTaskFriendlyName, typeof(MonitorLunchAppointmentsTask).FullName, (uint)frequency);
                        UpdateStatus("Background Task Running");
                        break;

                    // If we were denied access, show the the reason to the user
                    case BackgroundAccessStatus.DeniedBySystemPolicy:
                        UpdateStatus($"Background Task Disabled by System Policy", false);
                        await new MessageDialog("The app has denied from adding a background task due to System Policy. This is usually because there are too many background tasks already. " + "r\n\nGo to Phone Settings > Background Apps and free up a couple slots.").ShowAsync();
                        break;
                    case BackgroundAccessStatus.Unspecified:
                        UpdateStatus($"Background Task Disabled", false);
                        await new MessageDialog("You did not make a choice. If you want to get reminders for upcoming lunches, please try again.").ShowAsync();
                        break;
                    case BackgroundAccessStatus.DeniedByUser:
                        UpdateStatus($"Background Task Disabled", false);
                        await new MessageDialog("You've blocked background tasks for this app or you have too many background tasks already. " + "r\n\nGo to Phone Settings > Background Apps \r\n\nFind this app in the list and re-enable background tasks.").ShowAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EnableBackgroundTask Exception: {ex}");
            }
            finally
            {
                UpdateStatus("", false);
            }
        }

        private async void DisableBackgroundTask()
        {
            try
            {
                IsBusy = true;
                IsBusyMessage = "unregistering background task...";

                if (await BackgroundTaskHelpers.UnregisterTaskAsync(MonitorLunchesTaskFriendlyName))
                    UpdateStatus("Reminders Background Task Disabled", false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DisableBackgroundTask Exception: {ex}");
            }
            finally
            {
                IsBusy = false;
                IsBusyMessage = "";
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

        #region Event Handlers

        public void GetStatusButton_OnClick(object sender, RoutedEventArgs e)
        {
            object obj;
            if (localSettings != null && localSettings.Values.TryGetValue("BgTaskStatus", out obj))
            {
                LastTaskStatusMessage = obj.ToString();
            }
        }

        #endregion
    }
}
