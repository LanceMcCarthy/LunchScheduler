using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
        private ApplicationDataContainer localSettings;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            if(ApplicationData.Current.LocalSettings != null)
                localSettings = ApplicationData.Current.LocalSettings;

            try
            {
                // UWP Community Toolkit
                var json = await StorageFileHelper.ReadTextFromLocalFileAsync(Constants.LunchAppointmentsFileName);

                Debug.WriteLine($"Appointments File Loaded");

                var appointments = JsonConvert.DeserializeObject<ObservableCollection<LunchAppointment>>(json);

                if (appointments == null || appointments.Count < 1)
                {
                    LogStatus($"No Appointments found");
                    return;
                }
                else
                {
                    LogStatus($"{appointments.Count} Appointments Found");
                }
                
                // Get the user's setting for how far ahead to check if there's an appointment
                int monitorTimeWindow = 30;
                object obj;
                if (localSettings != null && localSettings.Values.TryGetValue(Constants.SelectedMonitorTimeWindowSettingsKey, out obj))
                {
                    monitorTimeWindow = (int)obj;
                }

                foreach (var lunch in appointments)
                {
                    // If the lunch appointment is within user's SelectedMonitorTimeWindow, create a notification
                    if (DateTime.Now - lunch.LunchTime < TimeSpan.FromMinutes(monitorTimeWindow))
                    {
                        Debug.WriteLine($"Creating Toast for {lunch.Title}");

                        var visual = GenerateToastVisual(lunch);

                        var toastContent = new ToastContent
                        {
                            Visual = visual,
                            // Toast Args. The app will navigate
                            // to the LunchDetailPage when opened 
                            // from a toast using the appointment ID
                            Launch = $"?id={lunch.Id}"
                        };

                        var toast = new ToastNotification(toastContent.GetXml())
                        {
                            ExpirationTime = DateTime.Now.AddMinutes(30),
                            Tag = $"{lunch.Id}",
                            Group = "upcomingLunches"
                        };

                        ToastNotificationManager.CreateToastNotifier().Show(toast);

                        LogStatus($"Sent Toast for {lunch.Title}");
                    }
                }

                LogStatus($"Successful, last run {DateTime.Now:U}");
            }
            catch (FileNotFoundException)
            {
                LogStatus($"No Appointments File Saved. Add appointments before enabling reminders.");
            }
            catch (Exception ex)
            {
                LogStatus($"Error: {ex.Message}");
            }
            finally
            {
                deferral.Complete();
            }
        }

        /// <summary>
        /// Creates a ToastNotification Visual (text, image, etc.)
        /// </summary>
        /// <param name="lunch">Appointment to create a visual for</param>
        /// <returns>A visuial with either the message or a message and photo of a guest</returns>
        private ToastVisual GenerateToastVisual(LunchAppointment lunch)
        {
            if (lunch.Guests.Count > 0)
            {
                string message = $"You have lunch at {lunch.LunchTime:t} with";

                for (int i = 0; i < lunch.Guests.Count - 1; i++)
                {
                    // Add the proper punctuation and grammar between guest names

                    if (i == 0) 
                        message += ": "; // First guest
                    else if (i == lunch.Guests.Count - 1)
                        message += " and "; // Last guest
                    else
                        message += ", ";

                    // Add the guest's name
                    message += $"{lunch.Guests[i].FullName}";
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

        /// <summary>
        /// Logs operational status to LocalSettings and Debug output window
        /// </summary>
        /// <param name="message">Status message of operation</param>
        private void LogStatus(string message)
        {
            if(localSettings!=null)
                localSettings.Values[Constants.BackgroundTaskStatusSettingsKey] = message;

#if DEBUG
            Debug.WriteLine($"Background Task Log - {message}");
#endif
        }
    }
}
