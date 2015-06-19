
using Abot.Poco;
using log4net;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Abot.Core
{
    public interface IWebContentExtractor
    {
        PageContent GetContent(WebResponse response);
    }

    [Serializable]
    public class WebContentExtractor : IWebContentExtractor
    {
        static ILog _logger = LogManager.GetLogger("AbotLogger");

        public PageContent GetContent(WebResponse response)
        {
            using (MemoryStream memoryStream = GetRawData(response))
            {
                String charset = GetCharsetFromHeaders(response);

                if (charset == null)
                    charset = GetCharsetFromBody(memoryStream);

                memoryStream.Seek(0, SeekOrigin.Begin);

                Encoding e = GetEncoding(charset);
                string content = "";
                using (StreamReader sr = new StreamReader(memoryStream, e))
                {
                    content = sr.ReadToEnd();
                }

                PageContent pageContent = new PageContent();
                pageContent.Bytes = memoryStream.ToArray();
                pageContent.Charset = charset;
                pageContent.Encoding = e;
                pageContent.Text = content;

                return pageContent;
            }
        }

        private string GetCharsetFromHeaders(WebResponse webResponse)
        {
            string charset = null;
            String ctype = webResponse.Headers["content-type"];
            if (ctype != null)
            {
                int ind = ctype.IndexOf("charset=");
                if (ind != -1)
                    charset = ctype.Substring(ind + 8);
            }
            return charset;
        }

        private string GetCharsetFromBody(MemoryStream rawdata)
        {
            String charset = null;

            MemoryStream ms = rawdata;
            ms.Seek(0, SeekOrigin.Begin);

            //Do not wrapp in closing statement to prevent closing of this stream
            StreamReader srr = new StreamReader(ms, Encoding.ASCII);
            String meta = srr.ReadToEnd();

            if (meta != null)
            {
                Match match = Regex.Match(meta, @"<meta.*charset=(.+)\/*>");
                if (match.Success)
                {
                    string matchStr = match.Groups[1].Value;

                    int endInd = GetFirstOccurrenceAboveNeg1(matchStr.IndexOf('"'), matchStr.IndexOf('\''));
                    if (endInd == -1)
                        endInd = matchStr.IndexOf('\'');

                    if (endInd != -1)
                        charset = matchStr.Remove(endInd);
                }
            }

            return charset;
        }

        private int GetFirstOccurrenceAboveNeg1(int firstIndex, int secondIndex)
        {
            if (firstIndex > -1 && secondIndex > -1)
                return Math.Min(firstIndex, secondIndex);

            if (firstIndex < 0 && secondIndex < 0)
                return -1;

            return Math.Max(firstIndex, secondIndex);
        }


        private Encoding GetEncoding(string charset)
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

        private MemoryStream GetRawData(WebResponse webResponse)
        {
            MemoryStream rawData = new MemoryStream();

            try
            {
                using (Stream rs = webResponse.GetResponseStream())
                {
                    byte[] buffer = new byte[1024];
                    int read = rs.Read(buffer, 0, buffer.Length);
                    while (read > 0)
                    {
                        rawData.Write(buffer, 0, read);
                        read = rs.Read(buffer, 0, buffer.Length);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.WarnFormat("Error occurred while downloading content of url {0}", webResponse.ResponseUri.AbsoluteUri);
                _logger.Warn(e);
            }

            return rawData;
        }
    }

}
