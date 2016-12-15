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
using System.Threading.Tasks;

namespace LunchScheduler.Data.Models
{
    public sealed class NotifyTaskCompletion<TResult> : ModelBase
    {
        #region Constructor

        public NotifyTaskCompletion(Task<TResult> task)
        {
            if (task == null)
                throw new NullReferenceException("task");

            Task = task;
            if (!task.IsCompleted)
            {
                var _ = WatchTaskAsync(task);
            }
        }

        #endregion

        #region Methods

        private async Task WatchTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
            }

            NotifyPropertyChanged(() => Status);
            NotifyPropertyChanged(() => IsCompleted);
            NotifyPropertyChanged(() => IsNotCompleted);

            if (task.IsCanceled)
            {
                NotifyPropertyChanged(() => IsCanceled);
            }
            else if (task.IsFaulted)
            {
                NotifyPropertyChanged(() => IsFaulted);
                NotifyPropertyChanged(() => Exception);
                NotifyPropertyChanged(() => InnerException);
                NotifyPropertyChanged(() => ErrorMessage);
            }
            else
            {
                NotifyPropertyChanged(() => IsSuccessfullyCompleted);
                NotifyPropertyChanged(() => Result);
            }
        }

        #endregion

        #region Properties

        public Task<TResult> Task { get; private set; }

        public TResult Result => Task.Status == TaskStatus.RanToCompletion ? Task.Result : default(TResult);

        public TaskStatus Status => Task.Status;

        public bool IsCompleted => Task.IsCompleted;

        public bool IsNotCompleted => !Task.IsCompleted;

        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

        public bool IsCanceled => Task.IsCanceled;

        public bool IsFaulted => Task.IsFaulted;

        public AggregateException Exception => Task.Exception;

        public Exception InnerException => Exception?.InnerException;

        public string ErrorMessage => InnerException?.Message;

        #endregion

    }
}
