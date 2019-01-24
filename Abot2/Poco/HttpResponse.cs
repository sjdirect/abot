using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Abot2.Poco
{

    public class HttpResponse
    {
        /// <summary>Constructs a response based on the received system http response.</summary>
        public HttpResponse(HttpResponseMessage response, CookieContainer cookies)
        {
            if (response == null)
                return;
            //response.Content.Headers.ContentType
            StatusCode = response.StatusCode;
            //ContentType = response.ContentType;
            //ContentLength = response.ContentLength;
            Headers = response.Headers;
            //CharacterSet = response.CharacterSet;
            //ContentEncoding = response.ContentEncoding;
            Cookies = cookies;
            //IsFromCache = response.IsFromCache;
            LastModified = new DateTime(response.Content.Headers.LastModified.Value);
            Method = response.RequestMessage.Method.ToString();
            ProtocolVersion = response.Version;
            ResponseUri = response.RequestMessage.RequestUri;//Todo invetigate this, should be response.ServerUri
            //Server = response.Server;
            StatusDescription = response.ReasonPhrase;

            //TODO check this all out
            //if (!IsMutuallyAuthenticatedImplemented.HasValue)
            //{
            //    try
            //    {
            //        IsMutuallyAuthenticated = response.IsMutuallyAuthenticated;
            //        IsMutuallyAuthenticatedImplemented = true;
            //    }
            //    catch (NotImplementedException e)
            //    {
            //        IsMutuallyAuthenticatedImplemented = false;
            //    }
            //}
            //IsMutuallyAuthenticated = IsMutuallyAuthenticatedImplemented.Value && response.IsMutuallyAuthenticated;
        }

        /// <summary>Constructs an empty response to be filled later.</summary>
        public HttpResponse()
        {
            Headers = new NameValueCollection();
        }

        /// <summary>Status code returned by the server</summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>Collection of headers in the response</summary>
        public HttpResponseHeaders Headers { get; set; }

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
    }
}
