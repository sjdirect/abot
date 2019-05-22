using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Abot2.Tests.Unit
{
    public class FakeHttpContent : HttpContent
    {
        public string Content { get; set; }

        public FakeHttpContent(string content)
        {
            Content = content;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var byteArray = Encoding.ASCII.GetBytes(Content);
            await stream.WriteAsync(byteArray, 0, Content.Length).ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = Content.Length;
            return true;
        }
    }
}
