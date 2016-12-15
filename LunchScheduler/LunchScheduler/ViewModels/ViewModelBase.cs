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
using LunchScheduler.Data.Models;
using LunchScheduler.Helpers;

namespace LunchScheduler.ViewModels
{
    public abstract class ViewModelBase : ModelBase
    {
        private NotifyTaskCompletion<bool> isInitialized;
        private bool isBusy;
        private string isBusyMessage;

        protected ViewModelBase()
        {
            if (DesignMode.DesignModeEnabled)
                return;
            
            // ReSharper disable once VirtualMemberCallInConstructor
            IsInitialized = new NotifyTaskCompletion<bool>(Init());
        }

        public NotifyTaskCompletion<bool> IsInitialized
        {
            get { return isInitialized; }
            set { SetProperty(ref isInitialized, value); }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { SetProperty(ref isBusy, value); }
        }

        public string IsBusyMessage
        {
            get { return isBusyMessage; }
            set { SetProperty(ref isBusyMessage, value); }
        }

        public abstract Task<bool> Init();

        public async Task UpdateBusyStatusAsync(string message, bool busy = true)
        {
            await TaskHelpers.CallOnUiThreadAsync(() =>
            {
                IsBusy = busy;
                IsBusyMessage = message;
            });
        }
    }
}
