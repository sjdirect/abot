using CsQuery;
using HtmlAgilityPack;
using log4net;
using System;
using System.Net;

namespace Abot.Poco
{
    public class CrawledPage : PageToCrawl
    {
        ILog _logger = LogManager.GetLogger(typeof(CrawledPage).FullName);

        Lazy<HtmlDocument> _htmlDocument;
        Lazy<CQ> _csQueryDocument;

        public CrawledPage(Uri uri)
            : base(uri)
        {
            RawContent = "";
            _htmlDocument = new Lazy<HtmlDocument>(() => InitializeHtmlAgilityPackDocument() );
            _csQueryDocument = new Lazy<CQ>(() => InitializeCsQueryDocument());
        }

        /// <summary>
        /// The raw content of the request
        /// </summary>
        public string RawContent { get; set; }

        /// <summary>
        /// Lazy loaded Html Agility Pack (http://htmlagilitypack.codeplex.com/) document that can be used to retrieve/modify html elements on the crawled page.
        /// </summary>
        public HtmlDocument HtmlDocument { get { return _htmlDocument.Value; } }

        /// <summary>
        /// Lazy loaded CsQuery (https://github.com/jamietre/CsQuery) document that can be used to retrieve/modify html elements on the crawled page.
        /// </summary>
        public CQ CsQueryDocument { get { return _csQueryDocument.Value;  } }

        /// <summary>
        /// Web request sent to the server
        /// </summary>
        public HttpWebRequest HttpWebRequest { get; set; }

        /// <summary>
        /// Web response from the server. NOTE: The Close() method has been called before setting this property.
        /// </summary>
        public HttpWebResponse HttpWebResponse { get; set; }

        /// <summary>
        /// The web exception that occurred during the crawl
        /// </summary>
        public WebException WebException { get; set; }

        public override string ToString()
        {
            if(HttpWebResponse == null)
                return Uri.AbsoluteUri;
            else
                return string.Format("{0}[{1}]", Uri.AbsoluteUri, (int)HttpWebResponse.StatusCode);
        }

        /// <summary>
        /// The actual byte size of the page's raw content. This property is due to the Content-length header being untrustable.
        /// </summary>
        public long PageSizeInBytes { get; set; }

        private CQ InitializeCsQueryDocument()
        {
            CQ csQueryObject;
            try
            {
                csQueryObject = CQ.Create(RawContent);
            }
            catch (Exception e)
            {
                csQueryObject = CQ.Create("");

                _logger.ErrorFormat("Error occurred while loading CsQuery object for Url [{0}]", Uri);
                _logger.Error(e);
            }
            return csQueryObject;
        }

        private HtmlDocument InitializeHtmlAgilityPackDocument()
        {
            HtmlDocument hapDoc = new HtmlDocument();
            hapDoc.OptionMaxNestedChildNodes = 5000;//did not make this an externally configurable property since it is really an internal issue to hap
            try
            {
                hapDoc.LoadHtml(RawContent);
            }
            catch (Exception e)
            {
                hapDoc.LoadHtml("");

                _logger.ErrorFormat("Error occurred while loading HtmlAgilityPack object for Url [{0}]", Uri);
                _logger.Error(e);
            }
            return hapDoc;
        }
    }
}
