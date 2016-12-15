using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;
using Windows.UI.Xaml;
using LunchScheduler.Data.Common;
using Microsoft.Toolkit.Uwp.Services.Facebook;
using Microsoft.Toolkit.Uwp.Services.Twitter;

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

        public bool IsLoggedIn
        {
            get { return isLoggedIn; }
            set { SetProperty(ref isLoggedIn, value); }
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
                if(localSettings != null)
                    localSettings.Values["ConnectedServiceName"] = value;
                SetProperty(ref connectedServiceName, value);
            }
        }

        public string ProfileUsername
        {
            get { return profileUsername; }
            set { SetProperty(ref profileUsername, value); }
        }

        public string ProfilePictureUrl
        {
            get { return profilePictureUrl; }
            set { SetProperty(ref profilePictureUrl, value); }
        }

        public override async Task<bool> Init()
        {
            try
            {
                switch (ConnectedServiceName)
                {
                    case "FaceBook":
                        await LogIntoFacebookAsync();
                        break;
                    case "Twitter":
                        LogIntoTwitter(null, null);
                        break;
                    case "":
                        break;
                    default:
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async void LogIntoFacebook(object sender, RoutedEventArgs args)
        {
            await LogIntoFacebookAsync();
        }

        public async void LogIntoTwitter(object sender, RoutedEventArgs args)
        {
            await LogIntoTwitterAsync();
        }

        private async Task LogIntoFacebookAsync()
        {
            try
            {
                FacebookService.Instance.Initialize(ServiceKeys.FacebookAppId);
                if (!await FacebookService.Instance.LoginAsync())
                {
                    return;
                }

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
                if (!await TwitterService.Instance.LoginAsync())
                {
                    return;
                }

                var user = await TwitterService.Instance.GetUserAsync();
                ProfilePictureUrl = user.ProfileImageUrl;

                ConnectedServiceName = "Twitter";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LogIntoTwitterAsync Exception: {ex}");
            }
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

        //public async Task PostMessage()
        //{
        //    //await FacebookService.Instance.PostToFeedAsync("Title", "Message", "Description", "Url");
        //    //await FacebookService.Instance.PostToFeedAsync("Title", "Message", "Description", "Picture Name", stream);
        //}
    }
}
