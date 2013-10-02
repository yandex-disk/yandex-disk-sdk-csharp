/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace Disk.SDK.Utils
{
    /// <summary>
    /// Represents the parser for response's results.
    /// </summary>
    internal static class ResponseParser
    {
        /// <summary>
        /// Parses the disk item.
        /// </summary>
        /// <param name="currentPath">The current path.</param>
        /// <param name="responseText">The response text.</param>
        /// <returns>The  parsed item.</returns>
        public static DiskItemInfo ParseItem(string currentPath, string responseText)
        {
            return ParseItems(currentPath, responseText).FirstOrDefault();
        }

        /// <summary>
        /// Parses the disk items.
        /// </summary>
        /// <param name="currentPath">The current path.</param>
        /// <param name="responseText">The response text.</param>
        /// <returns>The list of parsed items.</returns>
        public static IEnumerable<DiskItemInfo> ParseItems(string currentPath, string responseText)
        {
            var items = new List<DiskItemInfo>();
            var xmlBytes = Encoding.UTF8.GetBytes(responseText);
            using (var xmlStream = new MemoryStream(xmlBytes))
            {
                using (var reader = XmlReader.Create(xmlStream))
                {
                    DiskItemInfo itemInfo = null;
                    while (reader.Read())
                    {
                        if (reader.IsStartElement())
                        {
                            switch (reader.Name)
                            {
                                case "d:response":
                                    itemInfo = new DiskItemInfo();
                                    break;
                                case "d:href":
                                    reader.Read();
                                    itemInfo.FullPath = reader.Value;
                                    break;
                                case "d:creationdate":
                                    reader.Read();
                                    itemInfo.CreationDate = DateTime.Parse(reader.Value);
                                    break;
                                case "d:getlastmodified":
                                    reader.Read();
                                    itemInfo.LastModified = DateTime.Parse(reader.Value);
                                    break;
                                case "d:displayname":
                                    reader.Read();
                                    itemInfo.DisplayName = reader.Value;
                                    break;
                                case "d:getcontentlength":
                                    reader.Read();
                                    itemInfo.ContentLength = int.Parse(reader.Value);
                                    break;
                                case "d:getcontenttype":
                                    reader.Read();
                                    itemInfo.ContentType = reader.Value;
                                    break;
                                case "d:getetag":
                                    reader.Read();
                                    itemInfo.Etag = reader.Value;
                                    break;
                                case "d:collection":
                                    itemInfo.IsDirectory = true;
                                    break;
                                case "public_url":
                                    reader.Read();
                                    itemInfo.PublicUrl = reader.Value;
                                    break;
                            }
                        }
                        else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "d:response")
                        {
                            if (itemInfo.OriginalFullPath != currentPath)
                            {
                                items.Add(itemInfo);
                            }
                        }
                    }
                }
            }

            return items;
        }

        /// <summary>
        /// Parses the link.
        /// </summary>
        /// <param name="responseText">The response text.</param>
        /// <returns>The parsed link.</returns>
        public static string ParseLink(string responseText)
        {
            var xmlBytes = Encoding.UTF8.GetBytes(responseText);
            using (var xmlStream = new MemoryStream(xmlBytes))
            {
                using (var reader = XmlReader.Create(xmlStream))
                {
                    reader.ReadToFollowing("public_url");
                    var url = reader.ReadElementContentAsString();
                    return url;
                }
            }
        }

        /// <summary>
        /// Parses the token.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        /// <returns>The parsed access token.</returns>
        public static string ParseToken(Stream responseStream)
        {
            using (var reader = new StreamReader(responseStream))
            {
                var responseText = reader.ReadToEnd();
                return ParseToken(responseText);
            }
        }

        /// <summary>
        /// Parses the token.
        /// </summary>
        /// <param name="resultString">The result string.</param>
        /// <returns>The parsed access token.</returns>
        public static string ParseToken(string resultString)
        {
            return Regex.Match(resultString, WebdavResources.TokenRegexPattern).Value;
        }
    }
}