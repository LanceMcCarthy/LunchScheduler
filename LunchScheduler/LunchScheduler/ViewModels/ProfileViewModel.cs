using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using LunchScheduler.Data.Common;
using LunchScheduler.Data.Models;
using Microsoft.Toolkit.Uwp.Services.Facebook;
using Microsoft.Toolkit.Uwp.Services.Twitter;
using Template10.Mvvm;

namespace LunchScheduler.ViewModels
{
    public class ProfileViewModel : ViewModelBase
    {
        private readonly ApplicationDataContainer localSettings;
        private string connectedServiceName = "";
        private string profilePictureUrl;
        private bool isLoggedIn;
        private string profileUsername;

        public ProfileViewModel()
        {
            if (DesignMode.DesignModeEnabled)
                return;

            localSettings = ApplicationData.Current.LocalSettings;
        }

        #region Properties

        public bool IsLoggedIn
        {
            get { return isLoggedIn; }
            set { Set(ref isLoggedIn, value); }
        }

        public string ConnectedServiceName
        {
            get
            {
                if (!string.IsNullOrEmpty(connectedServiceName))
                    return connectedServiceName;

                // If the  value if empty, let's check to see if there is one in settings
                object storedConnectedServiceName;

                if (localSettings.Values.TryGetValue("ConnectedServiceName", out storedConnectedServiceName))
                {
                    connectedServiceName = storedConnectedServiceName.ToString();
                }

                return connectedServiceName;
            }
            set
            {
                if (localSettings != null)
                    localSettings.Values["ConnectedServiceName"] = value;
                Set(ref connectedServiceName, value);
            }
        }

        public string ProfileUsername
        {
            get { return profileUsername; }
            set { Set(ref profileUsername, value); }
        }

        public string ProfilePictureUrl
        {
            get { return profilePictureUrl; }
            set { Set(ref profilePictureUrl, value); }
        }
        

        #endregion
        
        #region Methods
        

        public override async Task OnNavigatedToAsync(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            try
            {
                switch (ConnectedServiceName)
                {
                    case "Facebook":
                        await LogIntoFacebookAsync();
                        break;
                    case "Twitter":
                        await LogIntoTwitterAsync();
                        break;
                    case "":
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ProfileViewModel OnNavigatedToAsync Exception: {ex}");
            }
        }

        private async Task LogIntoFacebookAsync()
        {
            try
            {
                FacebookService.Instance.Initialize(ServiceKeys.FacebookAppId);

                IsLoggedIn = await FacebookService.Instance.LoginAsync();

                if (!IsLoggedIn)
                    return;

                var photo = await FacebookService.Instance.GetUserPictureInfoAsync();

                ProfilePictureUrl = photo.Url;
                ProfileUsername = FacebookService.Instance.LoggedUser;

                ConnectedServiceName = "Facebook";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LogIntoFacebookAsync Exception: {ex}");
            }
        }

        private async Task LogIntoTwitterAsync()
        {
            try
            {
                TwitterService.Instance.Initialize(ServiceKeys.TwitterConsumerKey, ServiceKeys.TwitterConsumerSecret,
                    ServiceKeys.CallbackUri);

                IsLoggedIn = await TwitterService.Instance.LoginAsync();

                if (!IsLoggedIn)
                    return;
                
                var user = await TwitterService.Instance.GetUserAsync();
                ProfilePictureUrl = user.ProfileImageUrl;

                ConnectedServiceName = "Twitter";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LogIntoTwitterAsync Exception: {ex}");
            }
        }

        public async Task PostMessage(LunchAppointment lunch)
        {
            if (lunch == null)
                return;

            string message = $"Having lunch at {lunch.LunchTime:t} with";

            if (lunch.Guests.Count > 0)
            {
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
            }

            if (lunch.Location != null)
            {
                message += $" at {lunch.Location.Name}";
            }
            
            // PROBLEM, the helpers require a URL, which is irrelevant for this app, so I search Bing and use the first result
            if (ConnectedServiceName == "Facebook")
            {
                if (!IsLoggedIn)
                    await LogIntoFacebookAsync();

                //var searchConfig = new BingSearchConfig
                //{
                //    Country = BingCountry.UnitedStates,
                //    Language = BingLanguage.English,
                //    Query = lunch.Location.Name,
                //    QueryType = BingQueryType.Search
                //};

                //var result = await BingService.Instance.RequestAsync(searchConfig, 5);

                //var firstResult = result.FirstOrDefault();

                //if (firstResult == null)
                //    return;

                //await FacebookService.Instance.PostToFeedAsync(title, message, "", "");

                await FacebookService.Instance.PostToFeedWithDialogAsync("TEST", message, "http://www.windows.com");

                //await FacebookService.Instance.PostToFeedAsync("Title", "Message", "Description", "Url");
                //await FacebookService.Instance.PostToFeedAsync("Title", "Message", "Description", "Picture Name", stream);
            }

            if (ConnectedServiceName == "Twitter")
            {
                if (!IsLoggedIn)
                    await LogIntoTwitterAsync();

                await TwitterService.Instance.TweetStatusAsync("TEST: " + message);
            }
        }

        #endregion


        #region Event handlers

        public async void LogIntoFacebook(object sender, RoutedEventArgs args)
        {
            await LogIntoFacebookAsync();
        }

        public async void LogIntoTwitter(object sender, RoutedEventArgs args)
        {
            await LogIntoTwitterAsync();
        }

        public async void LogOut(object sender, RoutedEventArgs args)
        {
            if (ConnectedServiceName == "Facebook")
            {
                await FacebookService.Instance.LogoutAsync();
            }
            else if (ConnectedServiceName == "Twitter")
            {
                TwitterService.Instance.Logout();
            }

            IsLoggedIn = false;

            ProfilePictureUrl = "";
            ProfileUsername = "";
            ConnectedServiceName = "";
        }

        #endregion
    }
}
