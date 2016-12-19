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
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Xaml;
using LunchScheduler.Data.Common;
using LunchScheduler.ViewModels;
using LunchScheduler.Views;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using Template10.Common;
using Template10.Controls;

namespace LunchScheduler
{
    // Using Template10's Bootstrapper https://github.com/Windows-XAML/Template10/wiki
    sealed partial class App : BootStrapper
    {
        // Expose a reference to the content frame so that we can interact with the hamburger menu from anywhere
        public static Shell Shell { get; private set; }

        // This ViewModel is for the user's chosen social login/profile
        //private static ProfileViewModel profileViewModel;
        //public static ProfileViewModel ProfileViewModel
        //{
        //    get { return profileViewModel ?? (profileViewModel = new ProfileViewModel()); }
        //    private set { profileViewModel = value; }
        //}
        
        public App()
        {
            InitializeComponent();
            //Suspending += OnSuspending;
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
            Debug.WriteLine("RESTORE STATUS from " + args.PreviousExecutionState);

            switch (args.PreviousExecutionState)
            {
                case ApplicationExecutionState.Terminated:
                    //ProfileViewModel = await GetCachedViewModelAsync();
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
        
        //private async void OnSuspending(object sender, SuspendingEventArgs e)
        //{
        //    var deferral = e.SuspendingOperation.GetDeferral();

        //    //await CacheViewModelAsync();

        //    deferral.Complete();
        //}

        //private static async Task CacheViewModelAsync()
        //{
        //    try
        //    {
        //        var json = JsonConvert.SerializeObject(ProfileViewModel);

        //        // Check to see if file is there
        //        var file = ApplicationData.Current.TemporaryFolder.TryGetItemAsync(Constants.ProfileViewModelFileName);
        //        if (file == null)
        //            await ApplicationData.Current.TemporaryFolder.CreateFileAsync(Constants.ProfileViewModelFileName, CreationCollisionOption.ReplaceExisting);

        //        // Then save with UWP Community Toolkit helper
        //        await StorageFileHelper.WriteTextToLocalFileAsync(json, Constants.ProfileViewModelFileName);
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"CachedViewModelException: {ex}");
        //    }
        //}

        //private static async Task<ProfileViewModel> GetCachedViewModelAsync()
        //{
        //    try
        //    {
        //        var json = await StorageFileHelper.ReadTextFromLocalFileAsync(Constants.ProfileViewModelFileName);
                
        //        var persistedViewModel = JsonConvert.DeserializeObject<ProfileViewModel>(json);

        //        if (persistedViewModel == null)
        //        {
        //            return new ProfileViewModel();
        //        }

        //        return persistedViewModel;
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"GetCachedViewModelAsync: {ex}");
        //        return new ProfileViewModel();
        //    }
        //}
    }
}
