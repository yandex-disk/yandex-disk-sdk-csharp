/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Navigation;

using Disk.SDK.Utils;

namespace Disk.SDK.Provider
{
    /// <summary>
    /// Represents wrapper for platform specific WebBrowser component.
    /// </summary>
    public class WebBrowserWrapper : IBrowser
    {
        private readonly WebBrowser browser;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebBrowserWrapper"/> class.
        /// </summary>
        /// <param name="browser">The browser.</param>
        public WebBrowserWrapper(WebBrowser browser)
        {
            this.browser = browser;
            this.browser.Navigating += this.BrowserOnNavigating;
        }

        /// <summary>
        /// Navigates to the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        public void Navigate(string url)
        {
            this.browser.Navigate(new Uri(url));
        }

        /// <summary>
        /// Occurs just before navigation to a document.
        /// </summary>
        public event EventHandler<GenericSdkEventArgs<string>> Navigating;

        /// <summary>
        /// Occurs when browser is navigating to the url.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="NavigatingCancelEventArgs"/> instance containing the event data.</param>
        private void BrowserOnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            this.Navigating.SafeInvoke(this, new GenericSdkEventArgs<string>(e.Uri.ToString()));
        }
    }
}