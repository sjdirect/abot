using System.Text;

namespace Abot.Poco
{
    public class PageContent
    {
        public PageContent()
        {
            Text = "";
        }

        /// <summary>
        /// The raw bytes data of the web response
        /// </summary>
        public byte[] Bytes { get; set; }

        /// <summary>
        /// String representation of the charset/encoding
        /// </summary>
        public string Charset { get; set; }

        /// <summary>
        /// The encoding of the web response
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// The raw text of the web response
        /// </summary>
        public string Text { get; set; }
    }
}
