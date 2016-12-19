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
                UpdateStatus("loading...");

                LunchAppointments = await FileHelpers.LoadLunchAppointments();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OnNavigatedToAsync Exception: {ex}");
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
