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
using System.Collections.ObjectModel;
using LunchScheduler.Data.Models;

namespace LunchScheduler.Helpers
{
    public static class DesignTimeHelpers
    {
        public static LunchAppointment GenerateSampleAppointment()
        {
            return new LunchAppointment
            {
                Title = $"Lunch with coworkers",
                LunchTime = DateTime.Now,
                Location = new LunchLocation
                {
                    Name = $"1st Floor Cafe",
                    Address = "1 Washington St, Boston MA"
                },
                Guests = GenerateSampleGuests()
            };
        }

        public static ObservableCollection<LunchAppointment> GenerateSampleAppointments()
        {
            var appts = new ObservableCollection<LunchAppointment>();

            for (int i = 0; i < 5; i++)
            {
                appts.Add(new LunchAppointment
                {
                    Title = $"Sample Appointment {i}",
                    LunchTime = DateTime.Now.AddDays(i),
                    Location = new LunchLocation
                    {
                        Name = $"Restaurant {i} on Main",
                        Address = "100 Main Street, Boston, MA"
                    },
                    Guests = GenerateSampleGuests()
                });
            }

            return appts;
        }

        public static ObservableCollection<LunchGuest> GenerateSampleGuests()
        {
            var contacts = new ObservableCollection<LunchGuest>();

            for (int i = 0; i < 3; i++)
            {
                contacts.Add(new LunchGuest
                {
                    Id = $"contact{i}",
                    FullName = $"Friend {i}",
                    ThumbnailUri = "ms-appx:///Assets/PlaceholderImages/Friend_Thumb.jpg",
                    PhoneNumbers = new List<LunchGuestPhoneNumber>()
                    {
                        new LunchGuestPhoneNumber
                        {
                            Description = "Mobile",
                            Number = "555-212-1212"
                        },
                        new LunchGuestPhoneNumber
                        {
                            Description = "Work",
                            Number = "555-212-1212"
                        }
                    },
                    EmailAddresses = new List<LunchGuestEmailAddress>
                    {
                        new LunchGuestEmailAddress
                        {
                            Description = "Personal",
                            Address = "john.doe@outlook.com"
                        },
                        new LunchGuestEmailAddress
                        {
                            Description = "Work",
                            Address = "john.doe@microsoft.com"
                        }
                    }
                });
            }

            return contacts;
        }
    }
}
