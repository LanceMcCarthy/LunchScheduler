using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Notifications;
using LunchScheduler.Data.Common;
using LunchScheduler.Data.Models;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;

namespace LunchScheduler.BackgroundTasks
{
    public sealed class MonitorLunchAppointmentsTask : IBackgroundTask
    {
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();
            try
            {
                // UWP Community Toolkit
                var json = await StorageFileHelper.ReadTextFromLocalFileAsync(Constants.LunchAppointmentsFileName);

                Debug.WriteLine($"--Load-- Lunch Appointments JSON:\r\n{json}");

                var appointments = JsonConvert.DeserializeObject<ObservableCollection<LunchAppointment>>(json);

                if (appointments == null || appointments.Count < 1)
                    return;
                
                foreach (var lunch in appointments)
                {
                    if (DateTime.Now - lunch.LunchTime < TimeSpan.FromMinutes(30))
                    {
                        var visual = GenerateToastMessage(lunch);

                        var toastContent = new ToastContent
                        {
                            Visual = visual,
                            Launch = $"?id={lunch.Id}" // Args when the user opens app from toast
                        };

                        var toast = new ToastNotification(toastContent.GetXml())
                        {
                            ExpirationTime = DateTime.Now.AddMinutes(30),
                            Tag = $"{lunch.Id}",
                            Group = "upcomingLunches"
                        };

                        ToastNotificationManager.CreateToastNotifier().Show(toast);
                    }
                }
            }
            catch (Exception ex)
            {
                ApplicationData.Current.LocalSettings.Values["BgTaskStatus"] = $"Error: {ex.Message}";
            }
            finally
            {
                deferral.Complete();
            }
        }

        private ToastVisual GenerateToastMessage(LunchAppointment lunch)
        {
            if (lunch.Guests.Count > 0)
            {
                string message = $"You have lunch at {lunch.LunchTime:t} with";

                foreach (var lunchGuest in lunch.Guests)
                {
                    message += $" {lunchGuest.FullName}";
                }

                // Create and return a visual that has the guest's names and the first guest's photo
                return new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText { Text = "Lunch Time!" },
                            new AdaptiveText { Text = message, HintWrap = true },
                            new AdaptiveImage { Source = lunch.Guests[0].ThumbnailUri, HintCrop = AdaptiveImageCrop.Circle }
                        }
                    }
                };
            }

            // If there are no guests, just use the location name
            return new ToastVisual()
            {
                BindingGeneric = new ToastBindingGeneric()
                {
                    Children =
                    {
                        new AdaptiveText { Text = "Lunch Time!" },
                        new AdaptiveText { Text = $"You have lunch at {lunch.LunchTime:t} at {lunch.Location.Name}", HintWrap = true }
                    }
                }
            };
        }

    }
}
