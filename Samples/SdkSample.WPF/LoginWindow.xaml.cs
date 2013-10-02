using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows;

using Disk.SDK;
using Disk.SDK.Provider;

namespace SdkSample.WPF
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        /// TODO: Register your application https://oauth.yandex.ru/client/new and set here your application ID and return URL.

#error define your client_id and return_url
        private const string CLIENT_ID = "";
        private const string RETURN_URL = "";

        private readonly IDiskSdkClient sdkClient;

        public LoginWindow()
        {
            this.InitializeComponent();
        }

        public LoginWindow(IDiskSdkClient sdkClient)
            : this()
        {
            this.sdkClient = sdkClient;

            this.sdkClient.AuthorizeAsync(new WebBrowserWrapper(browser), CLIENT_ID, RETURN_URL, this.CompleteCallback);
        }

        private void CompleteCallback(object sender, GenericSdkEventArgs<string> e)
        {
            if (this.AuthCompleted != null)
            {
                this.AuthCompleted(this, new GenericSdkEventArgs<string>(e.Result));
            }

            this.Close();
        }

        public event EventHandler<GenericSdkEventArgs<string>> AuthCompleted;
    }
}