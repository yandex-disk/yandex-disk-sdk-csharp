using System;
using System.Windows.Navigation;

using Microsoft.Phone.Controls;

namespace SdkSample.WinPhone
{
    public partial class ImageViewer : PhoneApplicationPage
    {
        public ImageViewer()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            string path = string.Empty;
            if (this.NavigationContext.QueryString.TryGetValue("path", out path))
            {
                this.webBrowser.Navigate(new Uri(path, UriKind.Relative));
            }
        }
    }
}