using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Common;
using LunchScheduler.Data.Models;
using LunchScheduler.Helpers;
using LunchScheduler.Views;
using Microsoft.Toolkit.Uwp;
using Newtonsoft.Json;
using Template10.Common;
using Template10.Mvvm;

namespace LunchScheduler.ViewModels
{
    public class LunchesViewModel : ViewModelBase
    {
        private ObservableCollection<LunchAppointment> lunchAppointments;
        private bool isBusy;
        private string isBusyMessage;

        public LunchesViewModel()
        {
            if (DesignMode.DesignModeEnabled)
            {
                LunchAppointments = DesignTimeHelpers.GenerateSampleAppointments();
            }
        }

        #region Properties

        public ObservableCollection<LunchAppointment> LunchAppointments
        {
            get { return lunchAppointments ?? (lunchAppointments = new ObservableCollection<LunchAppointment>()); }
            set { Set(ref lunchAppointments, value); }
        }

        public bool IsBusy
        {
            get { return isBusy; }
            set { Set(ref isBusy, value); }
        }

        public string IsBusyMessage
        {
            get { return isBusyMessage; }
            set { Set(ref isBusyMessage, value); }
        }

        #endregion

        #region Methods

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            try
            {
                LunchAppointments = await LoadLunchAppointments();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LunchesViewModel OnNavigatedToAsync Exception: {ex}");
            }
        }

        /// <summary>
        /// Loads saved lunch appointments from file
        /// </summary>
        /// <returns>ObservableCollection of LunchAppointments</returns>
        public async Task<ObservableCollection<LunchAppointment>> LoadLunchAppointments()
        {
            try
            {
                UpdateStatus("loading lunch appointments...");

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
            finally
            {
                UpdateStatus("", false);
            }
        }

        /// <summary>
        /// Shows busy indicator
        /// </summary>
        /// <param name="message">busy message to show</param>
        /// <param name="showBusyIndicator">toggles the busy indicator's visibility</param>
        private void UpdateStatus(string message, bool showBusyIndicator = true)
        {
            IsBusy = showBusyIndicator;
            IsBusyMessage = message;
        }

        #endregion

        #region Event handlers

        public void UpcomingLunchesGridView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Any())
            {
                var item = e.AddedItems.FirstOrDefault();
                if (item != null)
                    BootStrapper.Current.NavigationService.Navigate(typeof(LunchDetailPage), item);
            }
        }

        public void AddAppointmentAppBarButton_OnClick(object sender, RoutedEventArgs e)
        {
            BootStrapper.Current.NavigationService.Navigate(typeof(LunchDetailPage), LunchAppointments);
        }
        

        #endregion
        
    }
}
