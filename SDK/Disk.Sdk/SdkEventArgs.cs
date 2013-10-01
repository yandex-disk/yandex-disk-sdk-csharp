/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;

namespace Disk.SDK
{
    /// <summary>
    /// Represents base entity to store SDK completed events' data.
    /// </summary>
    public class SdkEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the SDK exception.
        /// </summary>
        /// <value>
        /// The SDK exception.
        /// </value>
        public SdkException Error { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SdkEventArgs"/> class.
        /// </summary>
        /// <param name="exception">The exception object.</param>
        public SdkEventArgs(SdkException exception)
        {
            this.Error = exception;
        }
    }

    /// <summary>
    /// Represents entity to store SDK completed events' data.
    /// </summary>
    /// <typeparam name="TData">The type of the data.</typeparam>
    public class GenericSdkEventArgs<TData> : SdkEventArgs
    {
        /// <summary>
        /// Gets the result of the event.
        /// </summary>
        /// <value>
        /// The result of the event.
        /// </value>
        public TData Result { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericSdkEventArgs{TData}"/> class.
        /// </summary>
        /// <param name="content">The content of the event.</param>
        public GenericSdkEventArgs(TData content)
            : base(null)
        {
            this.Result = content;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericSdkEventArgs{TData}"/> class.
        /// </summary>
        /// <param name="exception">The exception object.</param>
        public GenericSdkEventArgs(SdkException exception)
            : base(exception)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericSdkEventArgs{TData}"/> class.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="exception">The exception.</param>
        public GenericSdkEventArgs(TData result, SdkException exception)
            : base(exception)
        {
            this.Result = result;
        }
    }
}