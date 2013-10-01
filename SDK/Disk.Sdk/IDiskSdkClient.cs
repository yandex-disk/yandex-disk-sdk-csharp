/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Disk.SDK.Provider")]

namespace Disk.SDK
{
    /// <summary>
    /// Represents interface to access the Disk SDK.
    /// </summary>
    public interface IDiskSdkClient
    {
        /// <summary>
        /// Occurs when the getting list operation completes.
        /// <param>The list of the folder's items.</param>
        /// </summary>
        event EventHandler<GenericSdkEventArgs<IEnumerable<DiskItemInfo>>> GetListCompleted;

        /// <summary>
        /// Occurs when the getting item operation completes.
        /// <param>The disk item's information.</param>
        /// </summary>
        event EventHandler<GenericSdkEventArgs<DiskItemInfo>> GetItemInfoCompleted;

        /// <summary>
        /// Occurs when the making folder operation completes.
        /// </summary>
        event EventHandler<SdkEventArgs> MakeFolderCompleted;

        /// <summary>
        /// Occurs when the removing item operation completes.
        /// </summary>
        event EventHandler<SdkEventArgs> RemoveCompleted;

        /// <summary>
        /// Occurs when the trashing item operation completes.
        /// </summary>
        event EventHandler<SdkEventArgs> TrashCompleted;

        /// <summary>
        /// Occurs when the moving item operation completes.
        /// </summary>
        event EventHandler<SdkEventArgs> MoveCompleted;

        /// <summary>
        /// Occurs when the copying item operation completes.
        /// </summary>
        event EventHandler<SdkEventArgs> CopyCompleted;

        /// <summary>
        /// Occurs when the un-publishing item operation completes.
        /// </summary>
        event EventHandler<SdkEventArgs> UnpublishCompleted;

        /// <summary>
        /// Occurs when the publishing operation completes.
        /// <param>The link to the published item.</param>
        /// </summary>
        event EventHandler<GenericSdkEventArgs<string>> PublishCompleted;

        /// <summary>
        /// Occurs when the checking access operation completes.
        /// <param>The link to the published item or empty string if item isn't published.</param>
        /// </summary>
        event EventHandler<GenericSdkEventArgs<string>> IsPublishedCompleted;

        /// <summary>
        /// The user access token.
        /// </summary>
        /// <value>The access token.</value>
        string AccessToken { get; }

        /// <summary>
        /// Gets a list of items from the specified directory asynchronously.
        /// Use the <see cref="GetListCompleted"/> event to get a result and handle the completion of the operation.
        /// </summary>
        /// <param name="path">The folder path.</param>
        void GetListAsync(string path = "/");

        /// <summary>
        /// Gets a paged list of items from the specified directory asynchronously.
        /// Use the <see cref="GetListCompleted"/> event to get a result and handle the completion of the operation.
        /// </summary>
        /// <param name="path">The folder path.</param>
        /// <param name="pageSize">The size of the page.</param>
        /// <param name="pageIndex">The index of the page.</param>
        void GetListPageAsync(string path, int pageSize, int pageIndex);

        /// <summary>
        /// Gets the disk item's information asynchronously.
        /// Use the <see cref="GetItemInfoCompleted"/> event to get a result and handle the completion of the operation.
        /// </summary>
        /// <param name="path">The path to the specified item.</param>
        void GetItemInfoAsync(string path);

        /// <summary>
        /// Makes a new directory by the specified path asynchronously.
        /// Use the <see cref="MakeFolderCompleted"/> event to handle the completion of the operation.
        /// </summary>
        /// <param name="fullPath">The full path to a new folder.</param>
        void MakeDirectoryAsync(string fullPath);

        /// <summary>
        /// Removes an item by the specified path asynchronously.
        /// Use the <see cref="RemoveCompleted"/> event to handle the completion of the operation.
        /// </summary>
        /// <param name="path">The full path to the folder.</param>
        void RemoveAsync(string path);

        /// <summary>
        /// Trash an item by the specified path asynchronously.
        /// Use the <see cref="TrashCompleted"/> event to handle the completion of the operation.
        /// </summary>
        /// <param name="path">The full path to the folder.</param>
        void TrashAsync(string path);

        /// <summary>
        /// Moves the specified items to a new location asynchronously.
        /// Use the <see cref="MoveCompleted"/> event to handle the completion of the operation.
        /// </summary>
        /// <param name="source">The path to source items.</param>
        /// <param name="destination">The destination path.</param>
        void MoveAsync(string source, string destination);

        /// <summary>
        /// Copies the specified items to a new location asynchronously.
        /// Use the <see cref="CopyCompleted"/> event to handle the completion of the operation.
        /// </summary>
        /// <param name="source">The path to source items.</param>
        /// <param name="destination">The destination path.</param>
        void CopyAsync(string source, string destination);

        /// <summary>
        /// Publishes an item by the specified path asynchronously.
        /// Use the <see cref="PublishCompleted"/> event to get a result and handle the completion of the operation.
        /// </summary>
        /// <param name="path">The path of the item.</param>
        void PublishAsync(string path);

        /// <summary>
        /// UnPublishes an item by the specified path asynchronously.
        /// Use the <see cref="UnpublishCompleted"/> event to handle the completion of the operation.
        /// </summary>
        /// <param name="path">The path of the item.</param>
        void UnpublishAsync(string path);

        /// <summary>
        /// Determines whether an item by the specified path is published asynchronously.
        /// Use the <see cref="IsPublishedCompleted"/> event to get a result and handle the completion of the operation.
        /// </summary>
        /// <param name="path">The path of the item.</param>
        void IsPublishedAsync(string path);
    }
}