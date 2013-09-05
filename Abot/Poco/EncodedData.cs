using System.Text;

namespace Abot.Poco
{
    public class EncodedData
    {
        /// <summary>
        /// String representation of the charset/encoding
        /// </summary>
        public string CharsetString { get; set; }

        /// <summary>
        /// The encoding of the web response
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// The raw content of the web response
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// The raw data of the web response
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// The actual byte size of the web response's content. This property is due to the Content-length header being untrustable.
        /// </summary>
        public long PageSizeInBytes { get; set; }
    }
}
