using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abot2.Poco;
using Serilog;

namespace Abot2.Core
{
    public interface IWebContentExtractor : IDisposable
    {
        Task<PageContent> GetContentAsync(HttpResponseMessage response);
    }

    public class WebContentExtractor : IWebContentExtractor
    {
        public virtual async Task<PageContent> GetContentAsync(HttpResponseMessage response)
        {
            var pageContent = new PageContent
            {
                Bytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false),
                Text = await response.Content.ReadAsStringAsync().ConfigureAwait(false)
            };

            pageContent.Charset = GetCharset(response.Content.Headers, pageContent.Text);
            pageContent.Encoding = GetEncoding(pageContent.Charset);

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
            catch
            {
                Log.Warning("Could not get Encoding for charset string [0]", charset);
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

        public virtual void Dispose()
        {
            // Nothing to do
        }
    }
}
