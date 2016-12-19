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

using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using LunchScheduler.Views;
using Template10.Common;
using Template10.Controls;

namespace LunchScheduler
{
    // Using Template10's Bootstrapper https://github.com/Windows-XAML/Template10/wiki
    sealed partial class App : BootStrapper
    {
        // Expose a reference to the content frame so that we can interact with the hamburger menu from anywhere
        public static Shell Shell { get; private set; }
        
        public App()
        {
            InitializeComponent();
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

        public override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            // If the app was launched from a reminder, it will have the appointment ID in the args
            if (args.Kind == ActivationKind.ToastNotification)
            {
                var toastArgs = args as ToastNotificationActivatedEventArgs;
                var argument = toastArgs?.Argument;

                if (argument != null && argument.Contains("id"))
                {
                    Debug.WriteLine($"OnActivated ToastNotification argument: {argument}");

                    NavigationService.Navigate(typeof(LunchesPage), argument);
                }
            }
            else
            {
                NavigationService.Navigate(typeof(LunchesPage));
            }

            return Task.CompletedTask;
        }
    }
}
