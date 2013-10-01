/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace Disk.SDK.Provider
{
    using Disk.SDK.Utils;

    /// <summary>
    /// Disk SDK extension methods with upload\download operations for .NET 4+ platform.
    /// </summary>
    public static class DiskSdkClientExtensions
    {
        private static string retUrl;
        private static EventHandler<GenericSdkEventArgs<string>> completeHandler;

        /// <summary>
        /// Authorizes the asynchronous.
        /// </summary>
        /// <param name="sdkClient">The SDK client.</param>
        /// <param name="browser">The browser.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <param name="completeCallback">The complete callback.</param>
        public static void AuthorizeAsync(this IDiskSdkClient sdkClient, IBrowser browser, string clientId, string returnUrl, EventHandler<GenericSdkEventArgs<string>> completeCallback)
        {
            retUrl = returnUrl;
            completeHandler = completeCallback;
            var authUrl = string.Format(WebdavResources.AuthBrowserUrlFormat, clientId);
            browser.Navigating += BrowserOnNavigating;
            browser.Navigate(authUrl);
        }

        /// <summary>
        /// Occurs when browser is navigating to the url.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event argument.</param>
        private static void BrowserOnNavigating(object sender, GenericSdkEventArgs<string> e)
        {
            if (e.Result.Contains(retUrl))
            {
                var token = ResponseParser.ParseToken(e.Result);
                completeHandler.SafeInvoke(sender, new GenericSdkEventArgs<string>(token));
            }
        }

        /// <summary>
        /// Downloads the file as an asynchronous operation.
        /// </summary>
        /// <param name="sdkClient">The SDK client.</param>
        /// <param name="path">The path to the file.</param>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="completeCallback">The complete callback.</param>
        public static void DownloadFileAsync(this IDiskSdkClient sdkClient, string path, Stream fileStream, IProgress progress, EventHandler<SdkEventArgs> completeCallback)
        {
            var request = HttpUtilities.CreateRequest(sdkClient.AccessToken, path);
            request.Method = WebdavResources.GetMethod;
            try
            {
                request.BeginGetResponse(
                    getResponseResult =>
                        {
                            var getResponseRequest = (HttpWebRequest)getResponseResult.AsyncState;
                            try
                            {
                                using (var response = getResponseRequest.EndGetResponse(getResponseResult))
                                {
                                    using (var responseStream = response.GetResponseStream())
                                    {
                                        const int BUFFER_LENGTH = 4096;
                                        var total = (ulong)response.ContentLength;
                                        ulong current = 0;
                                        var buffer = new byte[BUFFER_LENGTH];
                                        var count = responseStream.Read(buffer, 0, BUFFER_LENGTH);
                                        while (count > 0)
                                        {
                                            fileStream.Write(buffer, 0, count);
                                            current += (ulong)count;
                                            progress.UpdateProgress(current, total);
                                            count = responseStream.Read(buffer, 0, BUFFER_LENGTH);
                                        }

                                        fileStream.Dispose();
                                    }
                                }

                                completeCallback.SafeInvoke(sdkClient, new SdkEventArgs(null));
                            }
                            catch (Exception ex)
                            {
                                completeCallback.SafeInvoke(sdkClient, new SdkEventArgs(HttpUtilities.ProcessException(ex)));
                            }
                        },
                    request);
            }
            catch (Exception ex)
            {
                completeCallback.SafeInvoke(sdkClient, new SdkEventArgs(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// Uploads the file as an asynchronous operation.
        /// </summary>
        /// <param name="sdkClient">The SDK client.</param>
        /// <param name="path">The path to the file.</param>
        /// <param name="fileStream">The file stream.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="completeCallback">The complete callback.</param>
        public static void UploadFileAsync(this IDiskSdkClient sdkClient, string path, Stream fileStream, IProgress progress, EventHandler<SdkEventArgs> completeCallback)
        {
            var request = HttpUtilities.CreateRequest(sdkClient.AccessToken, path);
            request.Method = WebdavResources.PutMethod;
            request.AllowWriteStreamBuffering = false;
            request.SendChunked = true;
            try
            {
                request.BeginGetRequestStream(
                    getRequestStreamResult =>
                    {
                        var getRequestStreamRequest = (HttpWebRequest)getRequestStreamResult.AsyncState;
                        try
                        {
                            using (var requestStream = getRequestStreamRequest.EndGetRequestStream(getRequestStreamResult))
                            {
                                const int BUFFER_LENGTH = 4096;
                                var total = (ulong)fileStream.Length;
                                ulong current = 0;
                                var buffer = new byte[BUFFER_LENGTH];
                                var count = fileStream.Read(buffer, 0, BUFFER_LENGTH);
                                while (count > 0)
                                {
                                    requestStream.Write(buffer, 0, count);
                                    current += (ulong)count;
                                    progress.UpdateProgress(current, total);
                                    count = fileStream.Read(buffer, 0, BUFFER_LENGTH);
                                }

                                fileStream.Dispose();
                            }

                            getRequestStreamRequest.BeginGetResponse(
                                getResponseResult =>
                                    {
                                        var getResponseRequest = (HttpWebRequest)getResponseResult.AsyncState;
                                        try
                                        {
                                            using (getResponseRequest.EndGetResponse(getResponseResult))
                                            {
                                                completeCallback.SafeInvoke(sdkClient, new SdkEventArgs(null));
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            completeCallback.SafeInvoke(sdkClient, new SdkEventArgs(HttpUtilities.ProcessException(ex)));
                                        }
                                    },
                                getRequestStreamRequest);
                        }
                        catch (Exception ex)
                        {
                            completeCallback.SafeInvoke(sdkClient, new SdkEventArgs(HttpUtilities.ProcessException(ex)));
                        }
                    },
                    request);
            }
            catch (Exception ex)
            {
                completeCallback.SafeInvoke(sdkClient, new SdkEventArgs(HttpUtilities.ProcessException(ex)));
            }
        }
    }
}