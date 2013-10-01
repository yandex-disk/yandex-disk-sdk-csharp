/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System.Net;

namespace Disk.SDK.CommonServices
{
    /// <summary>
    /// Represents interface for platform specific functionality.
    /// </summary>
    internal interface ICommonService
    {
        /// <summary>
        /// Sets the custom header for web-request.
        /// </summary>
        /// <param name="request">The web-request.</param>
        void SetCustomHeader(WebRequest request);
    }
}