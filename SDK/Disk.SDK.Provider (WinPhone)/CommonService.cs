/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System.Net;

using Disk.SDK.CommonServices;

namespace Disk.SDK.Provider
{
    /// <summary>
    /// Represents implementation of the common services for Windows Phone platform.
    /// </summary>
    public class CommonService : ICommonService
    {
        /// <summary>
        /// Sets the custom header for web-request.
        /// </summary>
        /// <param name="request">The web-request.</param>
        public void SetCustomHeader(WebRequest request)
        {
            request.Headers["X-Yandex-SDK-Version"] = "winphone, 1.0";
        }
    }
}