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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
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

            if (ApplicationData.Current.LocalSettings != null)
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

                int badgeCount = 0;

                foreach (var lunch in appointments)
                {
                    var timeUntilAppointment = lunch.LunchTime - DateTimeOffset.Now;

                    // If the lunch appointment is within user's SelectedMonitorTimeWindow, create a notification
                    if (timeUntilAppointment < TimeSpan.FromMinutes(monitorTimeWindow) && timeUntilAppointment > TimeSpan.Zero)
                    {
                        Debug.WriteLine($"Creating Toast for {lunch.Title}");

                        badgeCount++;

                        // create toast notification
                        GenerateToastNotification(lunch);
                        
                        // Update Livetile / Lockscreen
                        UpdateTile(lunch);

                        LogStatus($"Sent Toast and Tile for {lunch.Title}");
                    }
                }

                // Update the app tile/lockscreen badge count
                if(badgeCount > 0)
                    UpdateBadge(badgeCount);

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
        /// <param name="lunch">Appointment to create a notification for</param>
        private void GenerateToastNotification(LunchAppointment lunch)
        {
            ToastVisual visual = null;

            if (lunch.Guests.Count > 0)
            {
                string message = $"You have lunch at {lunch.LunchTime:t} with";

                for (int i = 0; i < lunch.Guests.Count - 1; i++)
                {
                    // Add the proper punctuation and grammar between guest names
                    if (i == 0)
                        message += " "; // First guest
                    else if (i == lunch.Guests.Count - 1)
                        message += " and "; // Last guest
                    else
                        message += ", ";

                    // Add the guest's name
                    message += $"{lunch.Guests[i].FullName}";
                }

                // Limit the size of the string so that we stay under the 5kb payload limit
                if (message.Length > 80)
                    message = message.Substring(0, 79);

                // Create and return a visual that has the guest's names and the first guest's photo
                visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText { Text = "Lunch Time!" },
                            new AdaptiveText { Text = message, HintWrap = true, HintStyle = AdaptiveTextStyle.Caption},
                            new AdaptiveImage { Source = lunch.Guests[0].ThumbnailUri, HintCrop = AdaptiveImageCrop.Circle }
                        }
                    }
                };
            }
            else
            {
                // If there are no guests, just use the location name
                visual = new ToastVisual
                {
                    BindingGeneric = new ToastBindingGeneric
                    {
                        Children =
                        {
                            new AdaptiveText { Text = "Lunch Time!" },
                            new AdaptiveText { Text = $"You have lunch at {lunch.LunchTime:t}", HintWrap = true }
                        }
                    }
                };
            }
            
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
                Tag = $"{lunch.Id}"
            };

            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        private void UpdateTile(LunchAppointment lunch)
        {
            TileVisual visual = null;

            if (lunch.Guests.Count > 0)
            {
                // if there are guests build tile update with their images and names

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
                visual = new TileVisual
                {
                    TileMedium = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            Children =
                            {
                                new AdaptiveImage {Source = lunch.Guests[0].ThumbnailUri, HintCrop = AdaptiveImageCrop.Circle},
                                new AdaptiveText {Text = "Lunch Time!", HintStyle = AdaptiveTextStyle.Header},
                                new AdaptiveText
                                {
                                    Text = message.Substring(0,62),
                                    HintWrap = true,
                                    HintStyle = AdaptiveTextStyle.Body
                                }
                            }
                        }
                    },
                    TileWide = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            Children =
                            {
                                new AdaptiveImage {Source = lunch.Guests[0].ThumbnailUri, HintCrop = AdaptiveImageCrop.Circle},
                                new AdaptiveText {Text = "Lunch Time!", HintStyle = AdaptiveTextStyle.Header},
                                new AdaptiveText
                                {
                                    Text = message.Substring(0,133),
                                    HintWrap = true,
                                    HintStyle = AdaptiveTextStyle.Body
                                }
                            }
                        }
                    },
                    TileLarge = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            Children =
                            {
                                new AdaptiveImage {Source = lunch.Guests[0].ThumbnailUri, HintCrop = AdaptiveImageCrop.Circle},
                                new AdaptiveText {Text = "Lunch Time!", HintStyle = AdaptiveTextStyle.Header},
                                new AdaptiveText
                                {
                                    Text =message.Substring(0,81),
                                    HintWrap = true,
                                    HintStyle = AdaptiveTextStyle.Body
                                }
                            }
                        }
                    }
                };
            }
            else
            {
                // No guests
                visual = new TileVisual
                {
                    TileMedium = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            Children =
                            {
                                new AdaptiveText {Text = "Lunch Time!", HintStyle = AdaptiveTextStyle.Header},
                                new AdaptiveText
                                {
                                    Text = $"You have lunch at {lunch.LunchTime:t} at {lunch.Location.Name}",
                                    HintWrap = true,
                                    HintStyle = AdaptiveTextStyle.Body
                                }
                            }
                        }
                    },
                    TileWide = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            Children =
                            {
                                new AdaptiveText {Text = "Lunch Time!", HintStyle = AdaptiveTextStyle.Header},
                                new AdaptiveText
                                {
                                    Text = $"You have lunch at {lunch.LunchTime:t} at {lunch.Location.Name}",
                                    HintWrap = true,
                                    HintStyle = AdaptiveTextStyle.Body
                                }
                            }
                        }
                    },
                    TileLarge = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            Children =
                            {
                                new AdaptiveText {Text = "Lunch Time!", HintStyle = AdaptiveTextStyle.Header},
                                new AdaptiveText
                                {
                                    Text = $"You have lunch at {lunch.LunchTime:t} at {lunch.Location.Name}",
                                    HintWrap = true,
                                    HintStyle = AdaptiveTextStyle.Body
                                }
                            }
                        }
                    }
                };
            }
            
            var content = new TileContent()
            {
                Visual = visual
            };

            var notification = new TileNotification(content.GetXml());
            TileUpdateManager.CreateTileUpdaterForApplication().Update(notification);
        }

        private void UpdateBadge(int num)
        {
            var badgeXml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
            
            var badgeElement = badgeXml.SelectSingleNode("/badge") as XmlElement;

            if (badgeElement == null)
                return;

            badgeElement.SetAttribute("value", num.ToString());
            
            var badge = new BadgeNotification(badgeXml);
            
            var badgeUpdater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();
            
            badgeUpdater.Update(badge);

        }

        /// <summary>
        /// Logs operational status to LocalSettings and Debug output window
        /// </summary>
        /// <param name="message">Status message of operation</param>
        private void LogStatus(string message)
        {
            if (localSettings != null)
                localSettings.Values[Constants.BackgroundTaskStatusSettingsKey] = message;

#if DEBUG
            Debug.WriteLine($"Background Task Log - {message}");
#endif
        }
    }
}
