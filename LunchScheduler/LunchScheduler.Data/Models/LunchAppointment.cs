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
using System.Runtime.Serialization;

namespace LunchScheduler.Data.Models
{
    [DataContract]
    public class LunchAppointment : ModelBase
    {
        private string id;
        private string title;
        private LunchLocation location;
        private DateTimeOffset lunchTime = DateTimeOffset.Now;
        private ObservableCollection<LunchGuest> guests;

        public LunchAppointment()
        {
            id = Guid.NewGuid().ToString("N");
        }

        [DataMember]
        public string Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }

        [DataMember]
        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        [DataMember]
        public LunchLocation Location
        {
            get { return location ?? (location = new LunchLocation()); }
            set { SetProperty(ref location, value); }
        }

        [DataMember]
        public DateTimeOffset LunchTime
        {
            get { return lunchTime; }
            set { SetProperty(ref lunchTime, value); }
        }

        [DataMember]
        public ObservableCollection<LunchGuest> Guests
        {
            get { return guests ?? (guests = new ObservableCollection<LunchGuest>()); }
            set { SetProperty(ref guests, value); }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LunchAppointment))
                return false;
            
            return string.Equals(id, ((LunchAppointment)obj).id);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = id?.GetHashCode() ?? 0;
                return hashCode;
            }
        }
    }
}
