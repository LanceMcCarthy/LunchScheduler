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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Storage;
using Windows.Storage.Streams;
using LunchScheduler.Data.Common;
using LunchScheduler.Data.Models;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;

namespace LunchScheduler.Helpers
{
    public static class FileHelpers
    {
        /// <summary>
        /// Loads saved lunch appointments from file
        /// </summary>
        /// <returns>ObservableCollection of LunchAppointments</returns>
        public static async Task<ObservableCollection<LunchAppointment>> LoadLunchAppointments()
        {
            try
            {
                // UWP Community Toolkit
                var json = await StorageFileHelper.ReadTextFromLocalFileAsync(Constants.LunchAppointmentsFileName);

                Debug.WriteLine($"--Load-- Lunch Appointments JSON:\r\n{json}");

                var appointments = JsonConvert.DeserializeObject<ObservableCollection<LunchAppointment>>(json);

                return appointments;
            }
            catch (FileNotFoundException fnfException)
            {
                Debug.WriteLine($"LoadLunchAppointments FileNotFoundException: {fnfException}");
                return new ObservableCollection<LunchAppointment>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoadLunchAppointments Exception: {ex}");
                return new ObservableCollection<LunchAppointment>();
            }
        }

        /// <summary>
        /// Saves lunch appointments to storage
        /// </summary>
        /// <returns></returns>
        public static async Task SaveLunchAppointments(ObservableCollection<LunchAppointment> appointments)
        {
            try
            {
                var json = JsonConvert.SerializeObject(appointments);

                // UWP Community Toolkit
                await StorageFileHelper.WriteTextToLocalFileAsync(json, Constants.LunchAppointmentsFileName);

                Debug.WriteLine($"--SAVE-- Lunch Appointments JSON:\r\n{json}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveLunchAppointments Exception: {ex}");
            }
        }

        /// <summary>
        /// Save the Contact's thumbnail as a jpg in the app's storage
        /// </summary>
        /// <param name="contact">The Contact to save the thumbnail for</param>
        /// <returns>File path to the jpg image</returns>
        public static async Task<string> SaveThumbnailAsync(Contact contact)
        {
            try
            {
                if (contact.Thumbnail == null)
                    return "";

                using (var stream = await contact.Thumbnail.OpenReadAsync())
                {
                    byte[] buffer = new byte[stream.Size];
                    var readBuffer =
                        await stream.ReadAsync(buffer.AsBuffer(), (uint)buffer.Length, InputStreamOptions.None);

                    var file = await ApplicationData.Current.LocalFolder.CreateFileAsync($"{contact.FullName}_Thumb.jpg",
                                CreationCollisionOption.ReplaceExisting);

                    using (var fileStream = await file.OpenStreamForWriteAsync())
                    {
                        await fileStream.WriteAsync(readBuffer.ToArray(), 0, (int)readBuffer.Length);
                    }

                    Debug.WriteLine($"Thumbnail saved - Path: {file.Path}");

                    // Return the saved image's file path
                    return file.Path;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SaveThumbnailAsync Exception: {ex}");
                return "";
            }
        }

        /// <summary>
        /// Checks to see if there is a thumbnail photo and deletes it
        /// </summary>
        /// <param name="filePath">File path to lunch guest's photo</param>
        /// <returns></returns>
        public static async Task DeleteThumbnailAsync(string filePath)
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(filePath);

                if (file != null)
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeleteThumbnailAsync Exception: {ex}");
            }
        }
    }
}
