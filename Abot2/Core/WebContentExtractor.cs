using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Abot2.Poco;
using log4net;

namespace Abot2.Core
{
    public interface IWebContentExtractor : IDisposable
    {
        Task<PageContent> GetContentAsync(HttpResponseMessage response);
    }

    public class WebContentExtractor : IWebContentExtractor
    {
        static ILog _logger = LogManager.GetLogger(typeof(WebContentExtractor));

        public virtual async Task<PageContent> GetContentAsync(HttpResponseMessage response)
        {
            var pageContent = new PageContent();

            pageContent.Bytes = await response.Content.ReadAsByteArrayAsync();
            pageContent.Text = await response.Content.ReadAsStringAsync();

            var text2 = Encoding.UTF8.GetString(pageContent.Bytes, 0, pageContent.Bytes.Length);
            if (pageContent.Text != text2)
            {
                throw new Exception("ReadAsStringAsync and Encoding.UTF8.GetString should yield same result but did not.");
            }

            pageContent.Charset = GetCharset(response.Headers, pageContent.Text);
            pageContent.Encoding = GetEncoding(pageContent.Charset);


            return pageContent;

        }

        protected virtual string GetCharset(HttpResponseHeaders headers, string body)
        {
            var charset = GetCharsetFromHeaders(headers);
            if (charset == null)
            {
                charset = GetCharsetFromBody(body);
            }
            
            return CleanCharset(charset);
        }

        protected virtual string GetCharsetFromHeaders(HttpResponseHeaders headers)
        {
            string charset = null;
            IEnumerable<string> ctypes = null;
            if (headers.TryGetValues("content-type", out ctypes))
            {
                var ctype = ctypes.ElementAt(0);
                int ind = ctype.IndexOf("charset=", StringComparison.CurrentCultureIgnoreCase);
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
                Match match = Regex.Match(body, @"<meta(?!\s*(?:name|value)\s*=)(?:[^>]*?content\s*=[\s""']*)?([^>]*?)[\s""';]*charset\s*=[\s""']*([^\s""'/>]*)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    charset = string.IsNullOrWhiteSpace(match.Groups[2].Value) ? null : match.Groups[2].Value;
                }
            }

            return charset;
        }

        protected virtual Encoding GetEncoding(string charset)
        {
            Encoding e = Encoding.UTF8;
            if (charset != null)
            {
                try
                {
                    e = Encoding.GetEncoding(charset);
                }
                catch { }
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

        //private MemoryStream GetRawData(WebResponse webResponse)
        //{
        //    MemoryStream rawData = new MemoryStream();

        //    try
        //    {
        //        using (Stream rs = webResponse.GetResponseStream())
        //        {
        //            byte[] buffer = new byte[1024];
        //            int read = rs.Read(buffer, 0, buffer.Length);
        //            while (read > 0)
        //            {
        //                rawData.Write(buffer, 0, read);
        //                read = rs.Read(buffer, 0, buffer.Length);
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        _logger.WarnFormat("Error occurred while downloading content of url {0}", webResponse.ResponseUri.AbsoluteUri);
        //        _logger.Warn(e);
        //    }

        //    return rawData;
        //}

        public virtual void Dispose()
        {
            // Nothing to do
        }
    }
}
