/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;

namespace Disk.SDK.Utils
{
    /// <summary>
    /// Represents static class with SDK handlers' extensions.
    /// </summary>
    internal static class HandlerExtensions
    {
        /// <summary>
        /// Invokes the delegate <see cref="EventHandler{TEventArgs}"/> with null checking.
        /// </summary>
        /// <typeparam name="TEventArgs">The type of the parameter.</typeparam>
        /// <param name="handler">The typed event handler.</param>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The event argument.</param>
        public static void SafeInvoke<TEventArgs>(this EventHandler<TEventArgs> handler, object sender, TEventArgs args) where TEventArgs : EventArgs
        {
            if (handler != null)
            {
                handler(sender, args);
            }
        }
    }
}