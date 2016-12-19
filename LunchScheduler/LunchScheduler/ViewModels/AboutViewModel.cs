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
using Windows.ApplicationModel.Email;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Microsoft.Services.Store.Engagement;
using Microsoft.Toolkit.Uwp.Helpers;
using Template10.Mvvm;

namespace LunchScheduler.ViewModels
{
    public class AboutViewModel : ViewModelBase
    {
        private string appName;
        private string appVersion;
        private string operatingSystem;
        private string operatingSystemVersion;
        private string deviceFamily;
        private string deviceModel;
        private string deviceManufacturer;
        private string availableMemory;
        private Visibility feedbackHubButtonVisibility;
        private bool isBusy;
        private string isBusyMessage;

        public AboutViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                AppName = "Lunch Scheduler";
                AppVersion = "v. 1.0.0";
                DeviceFamily = "Mobile";
                OperatingSystem = "Windows 10";
                OperatingSystemVersion = $"v. 10.10493";
                AvailableMemory = $"3856 MB";
                DeviceModel = "Surface Book";
                DeviceManufacturer = "Microsoft";
                return;
            }
        }

        #region Properties

        public string AppName
        {
            get { return appName; }
            set { Set(ref appName, value); }
        }

        public string AppVersion
        {
            get{ return appVersion; }
            set { Set(ref appVersion, value); }
        }
        
        public string OperatingSystem
        {
            get { return operatingSystem;}
            set { Set(ref operatingSystem, value); }
        }

        public string OperatingSystemVersion
        {
            get { return operatingSystemVersion; }
            set { Set(ref operatingSystemVersion, value); }
        }
        
        public string DeviceFamily
        {
            get { return deviceFamily; }
            set { Set(ref deviceFamily, value); }
        }

        public string DeviceModel
        {
            get { return deviceModel; }
            set { Set(ref deviceModel, value); }
        }

        public string DeviceManufacturer
        {
            get { return deviceManufacturer; }
            set { Set(ref deviceManufacturer, value); }
        }

        public string AvailableMemory
        {
            get { return availableMemory; }
            set { Set(ref availableMemory, value); }
        }

        public Visibility FeedbackHubButtonVisibility
        {
            get { return feedbackHubButtonVisibility; }
            set { Set(ref feedbackHubButtonVisibility, value); }
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

                // show the Fewedback button if the user's device OS supports the FeedbackHub
                FeedbackHubButtonVisibility = StoreServicesFeedbackLauncher.IsSupported()
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                AppName = SystemInformation.ApplicationName;
                AppVersion = $"v. {SystemInformation.ApplicationVersion.Major}.{SystemInformation.ApplicationVersion.Minor}.{SystemInformation.ApplicationVersion.Build}";
                DeviceFamily = SystemInformation.DeviceFamily;
                OperatingSystem = SystemInformation.OperatingSystem;
                OperatingSystemVersion = $"v. {SystemInformation.OperatingSystemVersion.Major}.{SystemInformation.OperatingSystemVersion.Minor}.{SystemInformation.OperatingSystemVersion.Build}.{SystemInformation.OperatingSystemVersion.Revision}";
                AvailableMemory = $"{SystemInformation.AvailableMemory} MB";
                DeviceModel = SystemInformation.DeviceModel;
                DeviceManufacturer = SystemInformation.DeviceManufacturer;

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatedToAsync Exception: {ex}");
            }
            finally
            {
                UpdateStatus("");
            }

            return base.OnNavigatedToAsync(parameter, mode, state);
        }

        /// <summary>
        /// Shows busy indicator. Passing an empty string will hide the busy overlay
        /// </summary>
        /// <param name="message">busy message to show, leave empty to hide the overlay</param>
        private void UpdateStatus(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                IsBusy = false;
                IsBusyMessage = message;
            }
            else
            {
                IsBusy = true;
                IsBusyMessage = message;
            }
        }

        #endregion

        #region Event handlers

        public async void ContactDeveloperButton_OnClick(object sender, RoutedEventArgs e)
        {
            var emailMessage = new EmailMessage();

            var emailRecipient = new EmailRecipient("YOUR_EMAIL_ADDRESS@outlook.com");
            emailMessage.Subject = "Lunch Scheduler Support";
            emailMessage.Body = "[type your message here]";
            emailMessage.To.Add(emailRecipient);

            await EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }

        public async void LeaveFeedbackButton_OnClick(object sender, RoutedEventArgs e)
        {
            await StoreServicesFeedbackLauncher.GetDefault().LaunchAsync();
        }

        #endregion
    }
}