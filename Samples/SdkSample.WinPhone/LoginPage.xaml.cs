using System;
using System.Windows.Navigation;

using Disk.SDK;
using Disk.SDK.Provider;

using Microsoft.Phone.Controls;

namespace SdkSample.WinPhone
{
    public partial class LoginPage : PhoneApplicationPage
    {
        /// TODO: Register your application https://oauth.yandex.ru/client/new and set here your application ID and return URL.

#error define your client_id and return_url
        private const string CLIENT_ID = "";
        private const string RETURN_URL = "";

        private readonly IDiskSdkClient sdkClient;

        public LoginPage()
        {
            InitializeComponent();

            this.sdkClient = new DiskSdkClient();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.sdkClient.AuthorizeAsync(new WebBrowserWrapper(browser), CLIENT_ID, RETURN_URL, this.CompleteCallback);
        }

        private void CompleteCallback(object sender, GenericSdkEventArgs<string> e)
        {
            this.NavigationService.Navigate(new Uri("/MainPage.xaml?token=" + e.Result, UriKind.Relative));
        }
    }
}