using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Abot2.Poco;
using Serilog;

namespace Abot2.Core
{
    public interface IWebContentExtractor : IDisposable
    {
        /// <param name="response">The response whose content to read</param>
        /// <param name="cancellationToken">Cancels reading the content</param>
        Task<PageContent> GetContentAsync(HttpResponseMessage response, CancellationToken cancellationToken = default);
    }

    public class WebContentExtractor : IWebContentExtractor
    {
        public virtual async Task<PageContent> GetContentAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
        {
            //The cancellable overloads only exist on .NET 5 and later; older frameworks check the token between the reads
#if NET
            var pageContent = new PageContent
            {
                Bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false),
            };
            var contentText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            cancellationToken.ThrowIfCancellationRequested();
            var pageContent = new PageContent
            {
                Bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false),
            };
            cancellationToken.ThrowIfCancellationRequested();
            var contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
#endif

            pageContent.Charset = GetCharset(response.Content.Headers, contentText);
            pageContent.Encoding = GetEncoding(pageContent.Charset);

#if NET
            var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
#else
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            using (StreamReader sr = new StreamReader(contentStream, pageContent.Encoding))
            {
                pageContent.Text = sr.ReadToEnd();
            }

            return pageContent;
        }

        protected virtual string GetCharset(HttpContentHeaders headers, string body)
        {
            var charset = GetCharsetFromHeaders(headers);
            if (charset == null)
            {
                charset = GetCharsetFromBody(body);
            }

            return CleanCharset(charset);
        }

        protected virtual string GetCharsetFromHeaders(HttpContentHeaders headers)
        {
            string charset = null;
            if (headers.TryGetValues("content-type", out var ctypes))
            {
                var ctype = ctypes.ElementAt(0);
                var ind = ctype.IndexOf("charset=", StringComparison.CurrentCultureIgnoreCase);
                if (ind != -1)
                    charset = ctype.Substring(ind + 8);
            }
            return charset;
        }

        protected virtual string GetCharsetFromBody(string body)
        {
            string charset = null;
            if (body != null)
            {
                //find expression from : http://stackoverflow.com/questions/3458217/how-to-use-regular-expression-to-match-the-charset-string-in-html
                var match = Regex.Match(body, @"<meta(?!\s*(?:name|value)\s*=)(?:[^>]*?content\s*=[\s""']*)?([^>]*?)[\s""';]*charset\s*=[\s""']*([^\s""'/>]*)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    charset = string.IsNullOrWhiteSpace(match.Groups[2].Value) ? null : match.Groups[2].Value;
                }
            }

            return charset;
        }

        protected virtual Encoding GetEncoding(string charset)
        {
            var e = Encoding.UTF8;

            if (charset == null || charset.Trim() == string.Empty)
                return e;

            try
            {
                e = Encoding.GetEncoding(charset);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
            {
                Log.Warning("Could not get Encoding for charset string [{0}], using UTF-8", charset);
            }

            return e;
        }

        protected virtual string CleanCharset(string charset)
        {
            //TODO temporary hack, this needs to be a configurable value
            if (charset == "cp1251") //Russian, Bulgarian, Serbian cyrillic
                charset = "windows-1251";

            return charset;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Nothing to release here. Derived classes that own resources override this and call base.Dispose(disposing).
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
