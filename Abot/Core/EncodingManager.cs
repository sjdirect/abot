
using Abot.Poco;
using System;
using System.IO;
using System.Net;
using System.Text;
namespace Abot.Core
{
    public interface IEncodingManager
    {
        EncodedData GetEncodingData(WebResponse response);
    }

    public class EncodingManager : IEncodingManager
    {
        public EncodedData GetEncodingData(WebResponse response)
        {
            MemoryStream rawdata = GetRawData(response);

            String charset = GetCharsetFromHeaders(response);

            if (charset == null)
                charset = GetCharsetFromBody(rawdata);


            rawdata.Seek(0, SeekOrigin.Begin);

            Encoding e = GetEncoding(charset);
            string content = "";
            using (StreamReader sr = new StreamReader(rawdata, e))
            {
                content = sr.ReadToEnd();
            }

            EncodedData eData = new EncodedData();
            eData.CharsetString = charset;
            eData.Content = content;
            eData.Data = e.GetBytes(eData.Content);
            eData.PageSizeInBytes = eData.Data.Length;
            eData.Encoding = e;

            return eData;
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

            String meta = "";
            using (StreamReader srr = new StreamReader(ms, Encoding.ASCII))
            {
                meta = srr.ReadToEnd();
            }

            if (meta != null)
            {
                int start_ind = meta.IndexOf("charset=");
                int end_ind = -1;
                if (start_ind != -1)
                {
                    end_ind = meta.IndexOf("\"", start_ind);
                    if (end_ind != -1)
                    {
                        int start = start_ind + 8;
                        charset = meta.Substring(start, end_ind - start + 1);
                        charset = charset.TrimEnd(new Char[] { '>', '"' });
                    }
                }
            }

            return charset;
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
            MemoryStream rawdata = new MemoryStream();

            using (Stream rs = webResponse.GetResponseStream())
            {
                byte[] buffer = new byte[1024];
                int read = rs.Read(buffer, 0, buffer.Length);
                while (read > 0)
                {
                    rawdata.Write(buffer, 0, read);
                    read = rs.Read(buffer, 0, buffer.Length);
                }
            }

            return rawdata;
        }

    }

}
