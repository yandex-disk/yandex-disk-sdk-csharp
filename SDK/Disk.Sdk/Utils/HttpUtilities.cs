/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;
using System.IO;
using System.Net;
using System.Text;

using Disk.SDK.CommonServices;

namespace Disk.SDK.Utils
{
    /// <summary>
    /// Represents static access to the HTTP utilities.
    /// </summary>
    internal static class HttpUtilities
    {
        /// <summary>
        /// Creates the common web request.
        /// </summary>
        /// <param name="token">The access token.</param>
        /// <param name="path">The request path.</param>
        /// <returns>The common HttpWebRequest object.</returns>
        public static HttpWebRequest CreateRequest(string token, string path = "/")
        {
            var url = WebdavResources.ApiUrl + path;
            var request = WebRequest.CreateHttp(url);
            request.Accept = "*/*";
            request.Headers["Depth"] = "1";
            request.Headers["Authorization"] = "OAuth " + token;
            CommonServiceManager.CommonService.SetCustomHeader(request);
            return request;
        }

        /// <summary>
        /// Processes the exception.
        /// </summary>
        /// <param name="ex">The caught exception.</param>
        /// <returns>The clear SDK exception object.</returns>
        public static SdkException ProcessException(Exception ex)
        {
            var exception = new SdkException(ex.Message);
            if (ex is WebException)
            {
                var webEx = ex as WebException;
                var response = webEx.Response as HttpWebResponse;
                if (response != null)
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.Unauthorized:
                            exception = new SdkNotAuthorizedException();
                            break;
                        case HttpStatusCode.BadRequest:
                            exception = new SdkBadRequestException();
                            break;
                        case HttpStatusCode.NotFound:
                            exception = new SdkBadParameterException();
                            break;
                    }
                }
            }
            else if (ex is OutOfMemoryException)
            {
                exception = new SdkOutOfMemoryException();
            }

            return exception;
        }

        /// <summary>
        /// Sends the empty request.
        /// </summary>
        /// <param name="request">The http-request.</param>
        /// <param name="completedAction">The completed action.</param>
        public static void SendEmptyRequest(WebRequest request, Action<SdkException> completedAction)
        {
            try
            {
                request.BeginGetResponse(
                    getResponseResult =>
                        {
                            var getResponseRequest = (HttpWebRequest)getResponseResult.AsyncState;
                            try
                            {
                                using (getResponseRequest.EndGetResponse(getResponseResult))
                                {
                                    completedAction.Invoke(null);
                                }
                            }
                            catch (Exception ex)
                            {
                                completedAction(ProcessException(ex));
                            }
                        },
                    request);
            }
            catch (Exception ex)
            {
                completedAction(ProcessException(ex));
            }
        }

        /// <summary>
        /// Sends the full request.
        /// </summary>
        /// <param name="requestState">The request state.</param>
        /// <param name="processResponseAction">The process response action.</param>
        public static void SendFullRequest(RequestState requestState, Action<Stream, RequestState, SdkException> processResponseAction)
        {
            try
            {
                requestState.Request.BeginGetRequestStream(
                    getRequestStreamResult =>
                        {
                            var getRequestStreamState = (RequestState)getRequestStreamResult.AsyncState;
                            try
                            {
                                using (var requestStream = getRequestStreamState.Request.EndGetRequestStream(getRequestStreamResult))
                                {
                                    var postData = PrepareRequestContent(getRequestStreamState.RequestArgument);
                                    requestStream.Write(postData, 0, postData.Length);
                                }

                                getRequestStreamState.Request.BeginGetResponse(
                                    getResponseResult =>
                                        {
                                            var getResponseState = (RequestState)getResponseResult.AsyncState;
                                            try
                                            {
                                                using (var response = getResponseState.Request.EndGetResponse(getResponseResult))
                                                {
                                                    using (var responseStream = response.GetResponseStream())
                                                    {
                                                        processResponseAction(responseStream, getResponseState, null);
                                                    }
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                processResponseAction(null, getResponseState, ProcessException(ex));
                                            }
                                        },
                                    getRequestStreamState);
                            }
                            catch (Exception ex)
                            {
                                processResponseAction(null, requestState, ProcessException(ex));
                            }
                        },
                    requestState);
            }
            catch (Exception ex)
            {
                processResponseAction(null, requestState, ProcessException(ex));
            }
        }

        /// <summary>
        /// Converts the content string into byte array.
        /// </summary>
        /// <param name="requestContent">Content of the request.</param>
        /// <returns>The array of bytes.</returns>
        private static byte[] PrepareRequestContent(string requestContent)
        {
            return Encoding.UTF8.GetBytes(requestContent);
        }
    }

    /// <summary>
    /// Represents entity to store request's state.
    /// </summary>
    internal struct RequestState
    {
        /// <summary>
        /// Gets or sets the web-request.
        /// </summary>
        /// <value>The web-request.</value>
        public HttpWebRequest Request { get; set; }

        /// <summary>
        /// Gets or sets the web-request argument.
        /// </summary>
        /// <value>The web-request argument.</value>
        public string RequestArgument { get; set; }

        /// <summary>
        /// Gets or sets the web-response argument.
        /// </summary>
        /// <value>The web-response argument.</value>
        public string ResponseArgument { get; set; }
    }
}