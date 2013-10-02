/* Лицензионное соглашение на использование набора средств разработки
 * «SDK Яндекс.Диска» доступно по адресу: http://legal.yandex.ru/sdk_agreement
 */

using System;

namespace Disk.SDK
{
    /// <summary>
    /// Represents entity to store information about disk item.
    /// </summary>
    public class DiskItemInfo
    {
        /// <summary>
        /// Gets the original display name of the item.
        /// </summary>
        /// <value>The display name of the original.</value>
        public string OriginalDisplayName { get; private set; }
        
        /// <summary>
        /// Gets the original full path.
        /// </summary>
        /// <value>The original full path.</value>
        public string OriginalFullPath { get; private set; }

        /// <summary>
        /// Gets or sets the length of the content.
        /// </summary>
        /// <value>The length of the content.</value>
        public int ContentLength { get; set; }

        /// <summary>
        /// Gets or sets the type of the content (mime-type of the file).
        /// </summary>
        /// <value>The type of the content.</value>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the entity tag (MD5) of the file.
        /// </summary>
        /// <value>The entity tag.</value>
        public string Etag { get; set; }

        /// <summary>
        /// Gets or sets the public URL (empty string if the item isn't published).
        /// </summary>
        /// <value>The public URL.</value>
        public string PublicUrl { get; set; }

        /// <summary>
        /// Gets or sets the last modified date.
        /// </summary>
        /// <value>The last modified date.</value>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        /// <value>The creation date.</value>
        public DateTime CreationDate { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is directory.
        /// </summary>
        /// <value><c>true</c> if this instance is directory; otherwise, <c>false</c>.</value>
        public bool IsDirectory { get; set; }

        /// <summary>
        /// Gets or sets the decoded full path of the item.
        /// </summary>
        /// <value>The full path of the item.</value>
        public string FullPath
        {
            get { return Uri.UnescapeDataString(this.OriginalFullPath); }
            set { this.OriginalFullPath = value; }
        }

        /// <summary>
        /// Gets or sets the decoded display name of the item.
        /// </summary>
        /// <value>The display name of the item.</value>
        public string DisplayName
        {
            get { return Uri.UnescapeDataString(this.OriginalDisplayName); }
            set { this.OriginalDisplayName = value; }
        }

        /// <summary>
        /// Gets a value indicating whether this item is published.
        /// </summary>
        /// <value><c>true</c> if this item is published; otherwise, <c>false</c>.</value>
        public bool IsPublished
        {
            get { return !string.IsNullOrEmpty(this.PublicUrl); }
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == this.GetType() && this.Equals((DiskItemInfo)obj);
        }

        /// <summary>
        /// Compares with the specified item.
        /// </summary>
        /// <param name="other">The other item.</param>
        /// <returns><c>true</c> if paths of the items are equals, <c>false</c> otherwise.</returns>
        protected bool Equals(DiskItemInfo other)
        {
            return string.Equals(this.FullPath, other.FullPath);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (this.OriginalDisplayName != null ? this.OriginalDisplayName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.OriginalFullPath != null ? this.OriginalFullPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.ContentLength;
                hashCode = (hashCode * 397) ^ this.LastModified.GetHashCode();
                hashCode = (hashCode * 397) ^ this.CreationDate.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskItemInfo"/> class.
        /// </summary>
        public DiskItemInfo()
        {
            this.OriginalDisplayName = this.OriginalFullPath = this.PublicUrl = this.Etag = this.ContentType = string.Empty;
        }
    }
}