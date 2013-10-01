/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;

using Disk.SDK.Utils;

namespace Disk.SDK
{
    /// <summary>
    /// Represents base entity for all SDK exceptions.
    /// </summary>
    public class SdkException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Exception" /> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SdkException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Represents entity for not authorized exceptions.
    /// </summary>
    public class SdkNotAuthorizedException : SdkException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SdkNotAuthorizedException"/> class.
        /// </summary>
        public SdkNotAuthorizedException() : base(WebdavResources.NotAuthorizedErrorMessage)
        {
        }
    }

    /// <summary>
    /// Represents entity for bad parameters exception.
    /// </summary>
    public class SdkBadParameterException : SdkException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SdkBadParameterException"/> class.
        /// </summary>
        public SdkBadParameterException() : base(WebdavResources.BadParameterErrorMesage)
        {
        }
    }

    /// <summary>
    /// Represents entity for bad request exception.
    /// </summary>
    public class SdkBadRequestException : SdkException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SdkBadRequestException"/> class.
        /// </summary>
        public SdkBadRequestException() : base(WebdavResources.BadRequestErrorMessage)
        {
        }
    }

    /// <summary>
    /// Represents entity for provider exceptions.
    /// </summary>
    public class SdkProviderException : SdkException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SdkProviderException"/> class.
        /// </summary>
        public SdkProviderException() : base(WebdavResources.ProviderErrorMessage)
        {
        }
    }

    /// <summary>
    /// Represents entity for OutOfMemory exception.
    /// </summary>
    public class SdkOutOfMemoryException : SdkException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SdkOutOfMemoryException"/> class.
        /// </summary>
        public SdkOutOfMemoryException() : base(WebdavResources.OutOfMemoryErrorMessage)
        {
        }
    }
}