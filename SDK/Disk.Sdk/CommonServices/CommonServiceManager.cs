/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;
using System.Reflection;

namespace Disk.SDK.CommonServices
{
    /// <summary>
    /// Represents entry point for common service.
    /// </summary>
    internal static class CommonServiceManager
    {
        private const string ASSEMBLY_NAME              = "Disk.SDK.Provider",
                             SERVICE_NAME               = "Disk.SDK.Provider.CommonService",
                             SERVICE_FULL_NAME_FORMAT   = "{0}, {1}";

        private static ICommonService commonService;

        /// <summary>
        /// Creates the platform specific common service instance.
        /// </summary>
        /// <returns>The common service implementation.</returns>
        private static ICommonService CreateService()
        {
            var assemblyName = new AssemblyName { Name = ASSEMBLY_NAME };
            var commonServiceType = Type.GetType(string.Format(SERVICE_FULL_NAME_FORMAT, SERVICE_NAME, assemblyName.FullName), false);
            if (commonServiceType == null)
            {
                throw new SdkProviderException();
            }

            return (ICommonService)Activator.CreateInstance(commonServiceType);
        }

        /// <summary>
        /// Gets the platform specific common service instance.
        /// </summary>
        /// <value>
        /// The common service instance.
        /// </value>
        public static ICommonService CommonService
        {
            get
            {
                return commonService ?? (commonService = CreateService());
            }
        }
    }
}