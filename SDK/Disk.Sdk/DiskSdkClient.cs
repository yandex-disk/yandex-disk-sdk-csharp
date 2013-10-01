/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;
using System.Collections.Generic;
using System.IO;

namespace Disk.SDK
{
    using Disk.SDK.Utils;

    /// <summary>
    /// Represents entity to access the Disk SDK.
    /// </summary>
    public class DiskSdkClient : IDiskSdkClient
    {
        /// <summary>
        /// The user access token.
        /// </summary>
        private readonly string accessToken;

        /// <summary>
        /// Occurs when the getting list operation completes.
        /// <param>The list of the folder's items.</param>
        /// </summary>
        public event EventHandler<GenericSdkEventArgs<IEnumerable<DiskItemInfo>>> GetListCompleted;

        /// <summary>
        /// Occurs when the getting item operation completes.
        /// <param>The disk item's information.</param>
        /// </summary>
        public event EventHandler<GenericSdkEventArgs<DiskItemInfo>> GetItemInfoCompleted;

        /// <summary>
        /// Occurs when the making folder operation completes.
        /// </summary>
        public event EventHandler<SdkEventArgs> MakeFolderCompleted;

        /// <summary>
        /// Occurs when the removing item operation completes.
        /// </summary>
        public event EventHandler<SdkEventArgs> RemoveCompleted;

        /// <summary>
        /// Occurs when the trashing item operation completes.
        /// </summary>
        public event EventHandler<SdkEventArgs> TrashCompleted;

        /// <summary>
        /// Occurs when the moving item operation completes.
        /// </summary>
        public event EventHandler<SdkEventArgs> MoveCompleted;

        /// <summary>
        /// Occurs when the copying item operation completes.
        /// </summary>
        public event EventHandler<SdkEventArgs> CopyCompleted;

        /// <summary>
        /// Occurs when the un-publishing item operation completes.
        /// </summary>
        public event EventHandler<SdkEventArgs> UnpublishCompleted;

        /// <summary>
        /// Occurs when the publishing operation completes.
        /// <param>The link to the published item.</param>
        /// </summary>
        public event EventHandler<GenericSdkEventArgs<string>> PublishCompleted;

        /// <summary>
        /// Occurs when the checking access operation completes.
        /// <param>The link to the published item or empty string if item isn't published.</param>
        /// </summary>
        public event EventHandler<GenericSdkEventArgs<string>> IsPublishedCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskSdkClient"/> class.
        /// </summary>
        public DiskSdkClient()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskSdkClient"/> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public DiskSdkClient(string accessToken)
        {
            this.accessToken = accessToken;
        }

        /// <summary>
        /// The user access token.
        /// </summary>
        /// <value>The access token.</value>
        public string AccessToken
        {
            get { return this.accessToken; }
        }

        /// <summary>
        /// Gets a list of items from the specified folder.
        /// </summary>
        /// <param name="path">The folder path.</param>
        public void GetListAsync(string path = "/")
        {
            try
            {
                var request = HttpUtilities.CreateRequest(this.accessToken, path);
                request.Method = WebdavResources.PropfindMethod;
                var requestState = new RequestState { Request = request, RequestArgument = WebdavResources.ItemDetailsBody, ResponseArgument = path };
                HttpUtilities.SendFullRequest(requestState, this.ProcessGetListResponse);
            }
            catch (Exception ex)
            {
                this.GetListCompleted.SafeInvoke(this, new GenericSdkEventArgs<IEnumerable<DiskItemInfo>>(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// Gets a paged list of items from the  specified folder.
        /// </summary>
        /// <param name="path">The folder path.</param>
        /// <param name="pageSize">The size of the page.</param>
        /// <param name="pageIndex">The index of the page.</param>
        public void GetListPageAsync(string path, int pageSize, int pageIndex)
        {
            var param = pageIndex == 1
                            ? string.Concat("?amount=", pageSize)
                            : string.Concat("?offset=", pageIndex, "&amount=", pageSize);
            try
            {
                var request = HttpUtilities.CreateRequest(this.accessToken, path + param);
                request.Method = WebdavResources.PropfindMethod;
                var requestState = new RequestState { Request = request, RequestArgument = param, ResponseArgument = path };
                HttpUtilities.SendFullRequest(requestState, this.ProcessGetListResponse);
            }
            catch (Exception ex)
            {
                this.GetListCompleted.SafeInvoke(this, new GenericSdkEventArgs<IEnumerable<DiskItemInfo>>(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// Gets the disk item's information.
        /// </summary>
        /// <param name="path">The path to the specified item.</param>
        public void GetItemInfoAsync(string path)
        {
            try
            {
                var request = HttpUtilities.CreateRequest(this.accessToken, path);
                request.Method = WebdavResources.PropfindMethod;
                var requestState = new RequestState { Request = request, ResponseArgument = path };
                HttpUtilities.SendFullRequest(requestState, this.ProcessGetItemInfoResponse);
            }
            catch (Exception ex)
            {
                this.GetItemInfoCompleted.SafeInvoke(this, new GenericSdkEventArgs<DiskItemInfo>(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// Makes a new directory by the specified path.
        /// </summary>
        /// <param name="fullPath">The full path to a new folder.</param>
        public void MakeDirectoryAsync(string fullPath)
        {
            try
            {
                var request = HttpUtilities.CreateRequest(this.accessToken, fullPath);
                request.Method = WebdavResources.MakedirMethod;
                HttpUtilities.SendEmptyRequest(request, this.ProcessMakeDirectoryResponse);
            }
            catch (Exception ex)
            {
                this.MakeFolderCompleted.SafeInvoke(this, new GenericSdkEventArgs<DiskItemInfo>(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// Removes an item by the specified path.
        /// </summary>
        /// <param name="path">The full path to the folder.</param>
        public void RemoveAsync(string path)
        {
            try
            {
                var request = HttpUtilities.CreateRequest(this.accessToken, path);
                request.Method = WebdavResources.DeleteMethod;
                HttpUtilities.SendEmptyRequest(request, this.ProcessRemoveResponse);
            }
            catch (Exception ex)
            {
                this.RemoveCompleted.SafeInvoke(this, new SdkEventArgs(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// Trash an item by the specified path.
        /// </summary>
        /// <param name="path">The full path to the folder.</param>
        public void TrashAsync(string path)
        {
            const string TRASH_PARAM = "?trash=true";
            try
            {
                var request = HttpUtilities.CreateRequest(this.accessToken, path + TRASH_PARAM);
                request.Method = WebdavResources.DeleteMethod;
                HttpUtilities.SendEmptyRequest(request, this.ProcessTrashResponse);
            }
            catch (Exception ex)
            {
                this.TrashCompleted.SafeInvoke(this, new SdkEventArgs(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// Moves the specified source.
        /// </summary>
        /// <param name="source">The path to source items.</param>
        /// <param name="destination">The destination path.</param>
        public void MoveAsync(string source, string destination)
        {
            try
            {
                var request = HttpUtilities.CreateRequest(this.accessToken, source);
                request.Headers["Destination"] = destination;
                request.Method = WebdavResources.MoveMethod;
                HttpUtilities.SendEmptyRequest(request, this.ProcessMoveResponse);
            }
            catch (Exception ex)
            {
                this.MakeFolderCompleted.SafeInvoke(this, new SdkEventArgs(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// Copies the specified items to a new location.
        /// </summary>
        /// <param name="source">The path to source items.</param>
        /// <param name="destination">The destination path.</param>
        public void CopyAsync(string source, string destination)
        {
            try
            {
                var request = HttpUtilities.CreateRequest(this.accessToken, source);
                request.Headers["Destination"] = destination;
                request.Method = WebdavResources.CopyMethod;
                HttpUtilities.SendEmptyRequest(request, this.ProcessCopyResponse);
            }
            catch (Exception ex)
            {
                this.CopyCompleted.SafeInvoke(this, new SdkEventArgs(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// Publishes an item by the specified path.
        /// </summary>
        /// <param name="path">The path of the item.</param>
        public void PublishAsync(string path)
        {
            try
            {
                var request = HttpUtilities.CreateRequest(this.accessToken, path);
                request.Method = WebdavResources.ProppatchMethod;
                var requestState = new RequestState { Request = request, RequestArgument = WebdavResources.PublishBody };
                HttpUtilities.SendFullRequest(requestState, this.ProcessPublishResponse);
            }
            catch (Exception ex)
            {
                this.PublishCompleted.SafeInvoke(this, new GenericSdkEventArgs<string>(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// UnPublishes an item by the specified path.
        /// </summary>
        /// <param name="path">The path of the item.</param>
        public void UnpublishAsync(string path)
        {
            try
            {
                var request = HttpUtilities.CreateRequest(this.accessToken, path);
                request.Method = WebdavResources.ProppatchMethod;
                var requestState = new RequestState { Request = request, RequestArgument = WebdavResources.UnpublishBody };
                HttpUtilities.SendFullRequest(requestState, this.ProcessUnpublishResponse);
            }
            catch (Exception ex)
            {
                this.UnpublishCompleted.SafeInvoke(this, new SdkEventArgs(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// Determines whether an item by the specified path is published.
        /// </summary>
        /// <param name="path">The path of the item.</param>
        public void IsPublishedAsync(string path)
        {
            try
            {
                var request = HttpUtilities.CreateRequest(this.accessToken, path);
                request.Method = WebdavResources.PropfindMethod;
                var requestState = new RequestState { Request = request, RequestArgument = WebdavResources.CheckPublishingBody };
                HttpUtilities.SendFullRequest(requestState, this.ProcessIsPublishedResponse);
            }
            catch (Exception ex)
            {
                this.IsPublishedCompleted.SafeInvoke(this, new GenericSdkEventArgs<string>(HttpUtilities.ProcessException(ex)));
            }
        }

        /// <summary>
        /// Processes the GetList response.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        /// <param name="requestState">The state of the request.</param>
        /// <param name="sdkException">The SDK exception.</param>
        private void ProcessGetListResponse(Stream responseStream, RequestState requestState, SdkException sdkException)
        {
            if (sdkException == null)
            {
                using (var reader = new StreamReader(responseStream))
                {
                    var responseString = reader.ReadToEnd();
                    var items = ResponseParser.ParseItems(requestState.ResponseArgument, responseString);
                    this.GetListCompleted.SafeInvoke(this, new GenericSdkEventArgs<IEnumerable<DiskItemInfo>>(items));
                }
            }
            else
            {
                this.GetListCompleted.SafeInvoke(this, new GenericSdkEventArgs<IEnumerable<DiskItemInfo>>(sdkException));
            }
        }

        /// <summary>
        /// Processes the GetListPage response.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        /// <param name="requestState">The state of the request.</param>
        /// <param name="sdkException">The SDK exception.</param>
        private void ProcessGetItemInfoResponse(Stream responseStream, RequestState requestState, SdkException sdkException)
        {
            if (sdkException == null)
            {
                using (var reader = new StreamReader(responseStream))
                {
                    var responseString = reader.ReadToEnd();
                    var item = ResponseParser.ParseItem(requestState.ResponseArgument, responseString);
                    this.GetItemInfoCompleted.SafeInvoke(this, new GenericSdkEventArgs<DiskItemInfo>(item));
                }
            }
            else
            {
                this.GetItemInfoCompleted.SafeInvoke(this, new GenericSdkEventArgs<DiskItemInfo>(sdkException));
            }
        }

        /// <summary>
        /// Processes the Publish response.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        /// <param name="requestState">The state of the request.</param>
        /// <param name="sdkException">The SDK exception.</param>
        private void ProcessPublishResponse(Stream responseStream, RequestState requestState, SdkException sdkException)
        {
            if (sdkException == null)
            {
                using (var reader = new StreamReader(responseStream))
                {
                    var responseString = reader.ReadToEnd();
                    var link = ResponseParser.ParseLink(responseString);
                    this.PublishCompleted.SafeInvoke(this, new GenericSdkEventArgs<string>(link));
                }
            }
            else
            {
                this.PublishCompleted.SafeInvoke(this, new GenericSdkEventArgs<string>(sdkException));
            }
        }

        /// <summary>
        /// Processes the Un-publish response.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        /// <param name="requestState">The state of the request.</param>
        /// <param name="sdkException">The SDK exception.</param>
        private void ProcessUnpublishResponse(Stream responseStream, RequestState requestState, SdkException sdkException)
        {
            this.UnpublishCompleted.SafeInvoke(this, new SdkEventArgs(sdkException));
        }

        /// <summary>
        /// Processes the IsPublished response.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        /// <param name="requestState">The state of the request.</param>
        /// <param name="sdkException">The SDK exception.</param>
        private void ProcessIsPublishedResponse(Stream responseStream, RequestState requestState, SdkException sdkException)
        {
            if (sdkException == null)
            {
                using (var reader = new StreamReader(responseStream))
                {
                    var responseString = reader.ReadToEnd();
                    var link = ResponseParser.ParseLink(responseString);
                    this.IsPublishedCompleted.SafeInvoke(this, new GenericSdkEventArgs<string>(link));
                }
            }
            else
            {
                this.IsPublishedCompleted.SafeInvoke(this, new GenericSdkEventArgs<string>(sdkException));
            }
        }

        /// <summary>
        /// Processes the MakeDirectory response.
        /// </summary>
        /// <param name="sdkException">The SDK exception.</param>
        private void ProcessMakeDirectoryResponse(SdkException sdkException)
        {
            this.MakeFolderCompleted.SafeInvoke(this, new SdkEventArgs(sdkException));
        }

        /// <summary>
        /// Processes the Remove response.
        /// </summary>
        /// <param name="sdkException">The SDK exception.</param>
        private void ProcessRemoveResponse(SdkException sdkException)
        {
            this.RemoveCompleted.SafeInvoke(this, new SdkEventArgs(sdkException));
        }

        /// <summary>
        /// Processes the Trash response.
        /// </summary>
        /// <param name="sdkException">The SDK exception.</param>
        private void ProcessTrashResponse(SdkException sdkException)
        {
            this.TrashCompleted.SafeInvoke(this, new SdkEventArgs(sdkException));
        }

        /// <summary>
        /// Processes the Copy response.
        /// </summary>
        /// <param name="sdkException">The SDK exception.</param>
        private void ProcessCopyResponse(SdkException sdkException)
        {
            this.CopyCompleted.SafeInvoke(this, new SdkEventArgs(sdkException));
        }

        /// <summary>
        /// Processes the Move response.
        /// </summary>
        /// <param name="sdkException">The SDK exception.</param>
        private void ProcessMoveResponse(SdkException sdkException)
        {
            this.MoveCompleted.SafeInvoke(this, new SdkEventArgs(sdkException));
        }
    }
}