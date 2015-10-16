using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Abot.Poco
{
    /// <summary>Result of crawling a page</summary>
    /// <remarks>
    /// We use this wrapper class to enable using responses obtained by methods different than executing an HttpWebRequest.
    /// E.g. one may use a browser control embedded in the application to get a page content and construct an instance of this class
    /// to pass it to Abot.
    /// </remarks>
    public class HttpWebResponseWrapper
    {
        private HttpWebResponse InternalResponse;
        private byte[] Content;
        private Lazy<Stream> ContentStream;

        #region Constructors

        /// <summary>Constructs a response based on the received system http response.</summary>
        public HttpWebResponseWrapper(HttpWebResponse response)
        {
            this.InternalResponse = response;

            if (response == null)
                return;

            this.StatusCode = response.StatusCode;
            this.ContentType = response.ContentType;
            this.ContentLength = response.ContentLength;
            this.Headers = response.Headers;
            this.CharacterSet = response.CharacterSet;
            this.ContentEncoding = response.ContentEncoding;
            this.Cookies = response.Cookies;
            this.IsFromCache = response.IsFromCache;
            this.IsMutuallyAuthenticated = response.IsMutuallyAuthenticated;
            this.LastModified = GetLastModified(response);
            this.Method = response.Method;
            this.ProtocolVersion = response.ProtocolVersion;
            this.ResponseUri = response.ResponseUri;
            this.Server = response.Server;
            this.StatusDescription = response.StatusDescription;
        }

        private static DateTime GetLastModified(HttpWebResponse response)
        {
            try
            {
                return response.LastModified;
            }
            catch (ProtocolViolationException)
            {
                return DateTime.MinValue;
            }
        }

        /// <summary>Constructs a response based on custom parameters.</summary>
        /// <remarks>Recieves parameters neccesarily set for Abot to work.</remarks>
        public HttpWebResponseWrapper(HttpStatusCode statusCode, string contentType, byte[] content, NameValueCollection headers)
        {
            this.StatusCode = statusCode;
            this.Headers = headers;
            this.ContentType = contentType;
            this.ContentLength = content != null ? content.Length : 0;
            this.Content = content;
            this.ContentStream = new Lazy<Stream>(() => this.Content != null ? new MemoryStream(this.Content) : null);
        }

        /// <summary>Constructs an empty response to be filled later.</summary>
        public HttpWebResponseWrapper() { }

        #endregion

        #region Properties

        /// <summary>Status code returned by the server</summary>
        public HttpStatusCode StatusCode { get; set; }
        /// <summary>Server designated type of content</summary>
        public string ContentType { get; set; }
        /// <summary>Server designated length of content in bytes</summary>
        public long ContentLength { get; set; }
        /// <summary>Collection of headers in the response</summary>
        public NameValueCollection Headers { get; set; }
        /// <summary>Gets the character set of the response.</summary>
        public string CharacterSet { get; set; }
        /// <summary>Gets the method that is used to encode the body of the response.</summary>
        public string ContentEncoding { get; set; }
        /// <summary>Gets or sets the cookies that are associated with this response.</summary>
        public CookieCollection Cookies { get; set; }
        /// <summary>Was the response generated from the local cache?</summary>
        public bool IsFromCache { get; set; }
        /// <summary>Gets a System.Boolean value that indicates whether both client and server were authenticated.</summary>
        public bool IsMutuallyAuthenticated { get; set; }
        /// <summary>Gets the last date and time that the contents of the response were modified.</summary>
        public DateTime LastModified { get; set; }
        /// <summary>Gets the method that is used to return the response.</summary>
        public string Method { get; set; }
        /// <summary>Gets the version of the HTTP protocol that is used in the response.</summary>
        public Version ProtocolVersion { get; set; }
        /// <summary>Gets the URI of the Internet resource that responded to the request.</summary>
        public Uri ResponseUri { get; set; }
        /// <summary>Gets the name of the server that sent the response.</summary>
        public string Server { get; set; }
        /// <summary>Gets the status description returned with the response.</summary>
        public string StatusDescription { get; set; }

        #endregion

        #region Stream Methods

        /// <summary>Gets the actual response data.</summary>
        public Stream GetResponseStream()
        {
            return this.InternalResponse != null ?
                this.InternalResponse.GetResponseStream() :
                this.ContentStream.Value;
        }

        /// <summary>Gets the header with the given name.</summary>
        public string GetResponseHeader(string header)
        {
            return this.Headers != null ? this.Headers[header] : null;
        }

        #endregion
    }
}
