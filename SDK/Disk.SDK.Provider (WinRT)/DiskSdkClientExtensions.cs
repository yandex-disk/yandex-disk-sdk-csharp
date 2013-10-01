/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Disk.SDK.Utils;

using Windows.Networking.BackgroundTransfer;
using Windows.Security.Authentication.Web;
using Windows.Storage;

namespace Disk.SDK.Provider
{
    /// <summary>
    /// Disk SDK extension methods with upload\download operations for WinRT platform.
    /// </summary>
    public static class DiskSdkClientExtensions
    {
        /// <summary>
        /// Starts the asynchronous authentication operation.
        /// </summary>
        /// <param name="sdkClient">The SDK client.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns>The access token</returns>
        /// <exception cref="SdkException"/>
        public static async Task<string> AuthorizeAsync(this IDiskSdkClient sdkClient, string clientId, string returnUrl)
        {
            var requestUri = new Uri(string.Format(WebdavResources.AuthBrowserUrlFormat, clientId));
            var returnUri = new Uri(returnUrl);
            WebAuthenticationResult authResult = await WebAuthenticationBroker.AuthenticateAsync(WebAuthenticationOptions.None, requestUri, returnUri);
            if (authResult.ResponseStatus == WebAuthenticationStatus.Success)
            {
                return Regex.Match(authResult.ResponseData, WebdavResources.TokenRegexPattern).Value;
            }
            
            if (authResult.ResponseStatus == WebAuthenticationStatus.ErrorHttp)
            {
                throw new SdkException(authResult.ResponseErrorDetail.ToString());
            }
            
            throw new SdkException(authResult.ResponseStatus.ToString());
        }

        /// <summary>
        /// Starts to download the file as an asynchronous operation.
        /// </summary>
        /// <param name="sdkClient">The SDK client.</param>
        /// <param name="path">The path to the file.</param>
        /// <param name="file">The file.</param>
        /// <param name="progress">The progress handler.</param>
        public static async Task StartDownloadFileAsync(this IDiskSdkClient sdkClient, string path, IStorageFile file, IProgress progress)
        {
            try
            {
                var uri = new Uri(WebdavResources.ApiUrl + path);
                var downloader = new BackgroundDownloader();
                downloader.SetRequestHeader("Accept", "*/*");
                downloader.SetRequestHeader("TE", "chunked");
                downloader.SetRequestHeader("Accept-Encoding", "gzip");
                downloader.SetRequestHeader("Authorization", "OAuth " + sdkClient.AccessToken);
                downloader.SetRequestHeader("X-Yandex-SDK-Version", "winui, 1.0");
                var download = downloader.CreateDownload(uri, file);
                await HandleDownloadAsync(download, progress, true);
            }
            catch (Exception ex)
            {
                throw HttpUtilities.ProcessException(ex);
            }
        }

        /// <summary>
        /// Starts to upload the file as an asynchronous operation.
        /// </summary>
        /// <param name="sdkClient">The SDK client.</param>
        /// <param name="path">The path to the file.</param>
        /// <param name="file">The file.</param>
        /// <param name="progress">The progress.</param>
        public static async Task StartUploadFileAsync(this IDiskSdkClient sdkClient, string path, IStorageFile file, IProgress progress)
        {
            try
            {
                var uri = new Uri(WebdavResources.ApiUrl + path);
                var uploader = new BackgroundUploader { Method = "PUT" };
                uploader.SetRequestHeader("Authorization", "OAuth " + sdkClient.AccessToken);
                uploader.SetRequestHeader("X-Yandex-SDK-Version", "winui, 1.0");
                var upload = uploader.CreateUpload(uri, file);
                await HandleUploadAsync(upload, progress, true);
            }
            catch (Exception ex)
            {
                throw HttpUtilities.ProcessException(ex);
            }
        }

        /// <summary>
        /// Resumes specified download operation.
        /// Use <code>BackgroundDownloader.GetCurrentDownloadsAsync()</code> to get incomplete operations.
        /// </summary>
        /// <param name="downloadOperation">The download operation.</param>
        /// <param name="progress">The progress.</param>
        public static async Task ResumeDownloadAsync(DownloadOperation downloadOperation, IProgress progress)
        {
            await HandleDownloadAsync(downloadOperation, progress, false);
        }

        /// <summary>
        /// Resumes specified upload operation.
        /// Use <code>BackgroundUploader.GetCurrentDownloadsAsync()</code> to get incomplete operations.
        /// </summary>
        /// <param name="uploadOperation">The upload operation.</param>
        /// <param name="progress">The progress.</param>
        public static async Task ResumeUploadAsync(UploadOperation uploadOperation, IProgress progress)
        {
            await HandleUploadAsync(uploadOperation, progress, false);
        }

        /// <summary>
        /// Handles download operation.
        /// </summary>
        /// <param name="download">The download operation.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="start">if set to <c>true</c> starts a new operation, else attach to exist operation.</param>
        private static async Task HandleDownloadAsync(DownloadOperation download, IProgress progress, bool start)
        {
            try
            {
                var progressCallback = new Progress<DownloadOperation>(
                    operation =>
                        {
                            if (operation.Progress.TotalBytesToReceive > 0)
                            {
                                progress.UpdateProgress(operation.Progress.BytesReceived, operation.Progress.TotalBytesToReceive);
                            }
                        });

                if (start)
                {
                    await download.StartAsync().AsTask(progressCallback);
                }
                else
                {
                    await download.AttachAsync().AsTask(progressCallback);
                }
            }
            catch (Exception ex)
            {
                throw HttpUtilities.ProcessException(ex);
            }
        }

        /// <summary>
        /// Handles upload operation.
        /// </summary>
        /// <param name="upload">The upload operation.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="start">if set to <c>true</c> starts a new operation, else attach to exist operation.</param>
        private static async Task HandleUploadAsync(UploadOperation upload, IProgress progress, bool start)
        {
            try
            {
                var progressCallback = new Progress<UploadOperation>(
                    operation =>
                        {
                            if (operation.Progress.TotalBytesToSend > 0)
                            {
                                progress.UpdateProgress(operation.Progress.BytesSent, operation.Progress.TotalBytesToSend);
                            }
                        });

                if (start)
                {
                    await upload.StartAsync().AsTask(progressCallback);
                }
                else
                {
                    await upload.AttachAsync().AsTask(progressCallback);
                }
            }
            catch (Exception ex)
            {
                throw HttpUtilities.ProcessException(ex);
            }
        }
    }
}