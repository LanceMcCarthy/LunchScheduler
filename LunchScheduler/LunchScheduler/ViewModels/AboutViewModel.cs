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
using Windows.ApplicationModel;
using Windows.ApplicationModel.Email;
using Windows.UI.Xaml;
using Microsoft.Toolkit.Uwp.Helpers;

namespace LunchScheduler.ViewModels
{
    public class AboutViewModel : Template10.Mvvm.ViewModelBase
    {
        public AboutViewModel()
        {
            if (DesignMode.DesignModeEnabled)
                return;
        }

        public string AppName => SystemInformation.ApplicationName;

        public string AppVersion => $"v. {SystemInformation.ApplicationVersion.Major}.{SystemInformation.ApplicationVersion.Minor}.{SystemInformation.ApplicationVersion.Build}";

        public string DeviceFamily => SystemInformation.DeviceFamily;

        public string OperatingSystem => SystemInformation.OperatingSystem;

        public string OperatingSystemVersion => $"v. {SystemInformation.OperatingSystemVersion.Major}.{SystemInformation.OperatingSystemVersion.Minor}.{SystemInformation.OperatingSystemVersion.Build}.{SystemInformation.OperatingSystemVersion.Revision}";

        public string AvailableMemory => $"{SystemInformation.AvailableMemory} MB";

        public string DeviceModel => SystemInformation.DeviceModel;

        public string DeviceManufacturer => SystemInformation.DeviceManufacturer;
        
        public async void ContactDeveloperButton_OnClick(object sender, RoutedEventArgs e)
        {
            var emailMessage = new EmailMessage();

            var emailRecipient = new EmailRecipient("YOUR_EMAIL_ADDRESS@outlook.com");
            emailMessage.Subject = "Lunch Scheduler Support";
            emailMessage.Body = "[type your message here]";
            emailMessage.To.Add(emailRecipient);

            await EmailManager.ShowComposeNewEmailAsync(emailMessage);
        }
    }
}
