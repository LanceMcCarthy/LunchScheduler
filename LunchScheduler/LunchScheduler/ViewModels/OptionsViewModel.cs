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

using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using LunchScheduler.BackgroundTasks;
using Microsoft.Toolkit.Uwp;

namespace LunchScheduler.ViewModels
{
    public class OptionsViewModel : ViewModelBase
    {
        private readonly ApplicationDataContainer localSettings;
        private bool isBackgroundTaskEnabled;

        public OptionsViewModel()
        {
            if (DesignMode.DesignModeEnabled)
                return;

            localSettings = ApplicationData.Current.LocalSettings;
        }

        public bool IsBackgroundTaskEnabled
        {
            get
            {
                // check local settings first
                object obj;
                if (localSettings != null && localSettings.Values.TryGetValue("IsBackgroundTaskEnabled", out obj))
                {
                    if(obj is bool)
                        isBackgroundTaskEnabled = (bool)obj;
                }
                
                return isBackgroundTaskEnabled;
            }
            set
            {
                SetProperty(ref isBackgroundTaskEnabled, value);

                // Save the setting to local settings
                if (localSettings != null)
                    localSettings.Values["IsBackgroundTaskEnabled"] = value;

                // Enable or diable the task accordingly
                if (value)
                    EnableBackgroundTask();
                else
                    DisableBeckgroundTask();
            }
        }

        public override async Task<bool> Init()
        {
            
            return true;
        }

        private void EnableBackgroundTask()
        {
            // Using UWP Community Toolkit - http://docs.uwpcommunitytoolkit.com/en/master/helpers/BackgroundTaskHelper/
            BackgroundTaskRegistration registered = BackgroundTaskHelper.Register(typeof(MonitorLunchAppointmentsTask), new TimeTrigger(15, false));
            
        }

        private void DisableBeckgroundTask()
        {
            // Using UWP Community Toolkit - http://docs.uwpcommunitytoolkit.com/en/master/helpers/BackgroundTaskHelper/
            BackgroundTaskRegistration registered = BackgroundTaskHelper.Register(typeof(MonitorLunchAppointmentsTask), new TimeTrigger(15, false));
            registered.Unregister(false);
        }

    }
}
