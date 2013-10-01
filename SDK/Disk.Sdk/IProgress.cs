/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;

namespace Disk.SDK
{
    /// <summary>
    /// Represents interface to notify UI about async operation progress.
    /// </summary>
    public interface IProgress
    {
        /// <summary>
        /// Updates the progress value.
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="total">The total value.</param>
        void UpdateProgress(ulong current, ulong total);
    }

    /// <summary>
    /// Represents entity to notify UI about async operation progress.
    /// </summary>
    public class AsyncProgress : IProgress
    {
        /// <summary>
        /// The progress action.
        /// </summary>
        private readonly Action<ulong, ulong> progressAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncProgress"/> class.
        /// </summary>
        /// <param name="progressAction">The progress action.</param>
        public AsyncProgress(Action<ulong, ulong> progressAction)
        {
            this.progressAction = progressAction;
        }

        /// <summary>
        /// Updates the progress value.
        /// </summary>
        /// <param name="current">The current value.</param>
        /// <param name="total">The total value.</param>
        public void UpdateProgress(ulong current, ulong total)
        {
            this.progressAction(current, total);
        }
    }
}