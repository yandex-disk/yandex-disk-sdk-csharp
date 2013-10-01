/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;

namespace Disk.SDK
{
    /// <summary>
    /// Represents an abstraction for platform specific browser component.
    /// </summary>
    public interface IBrowser
    {
        /// <summary>
        /// Navigates to the specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        void Navigate(string url);

        /// <summary>
        /// Occurs just before navigation to a document.
        /// </summary>
        event EventHandler<GenericSdkEventArgs<string>> Navigating;
    }
}