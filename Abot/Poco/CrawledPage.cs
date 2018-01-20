using HtmlAgilityPack;
using log4net;
using System;
using System.Collections.Generic;
using System.Net;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;

namespace Abot.Poco
{
    [Serializable]
    public class CrawledPage : PageToCrawl
    {
        ILog _logger = LogManager.GetLogger("AbotLogger");
        HtmlParser _angleSharpHtmlParser;

        Lazy<HtmlDocument> _htmlDocument;
        Lazy<IHtmlDocument> _angleSharpHtmlDocument;

        public CrawledPage(Uri uri)
            : base(uri)
        {
            _htmlDocument = new Lazy<HtmlDocument>(InitializeHtmlAgilityPackDocument);
            _angleSharpHtmlDocument = new Lazy<IHtmlDocument>(InitializeAngleSharpHtmlParser);

            Content = new PageContent();
        }

        /// <summary>
        /// Lazy loaded Html Agility Pack (http://htmlagilitypack.codeplex.com/) document that can be used to retrieve/modify html elements on the crawled page.
        /// </summary>
        public HtmlDocument HtmlDocument { get { return _htmlDocument.Value; } }

        /// <summary>
        /// Lazy loaded AngleSharp IHtmlDocument (https://github.com/AngleSharp/AngleSharp) that can be used to retrieve/modify html elements on the crawled page.
        /// </summary>
        public IHtmlDocument AngleSharpHtmlDocument { get { return _angleSharpHtmlDocument.Value; } }

        /// <summary>
        /// Web request sent to the server
        /// </summary>
        public HttpWebRequest HttpWebRequest { get; set; }

        /// <summary>
        /// Web response from the server. NOTE: The Close() method has been called before setting this property.
        /// </summary>
        public HttpWebResponseWrapper HttpWebResponse { get; set; }

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
        /// Links parsed from page. This value is set by the WebCrawler.SchedulePageLinks() method only If the "ShouldCrawlPageLinks" rules return true or if the IsForcedLinkParsingEnabled config value is set to true.
        /// </summary>
        public IEnumerable<Uri> ParsedLinks { get; set; }

        /// <summary>
        /// The content of page request
        /// </summary>
        public PageContent Content { get; set; }

        /// <summary>
        /// A datetime of when the http request started
        /// </summary>
        public DateTime RequestStarted { get; set; }

        /// <summary>
        /// A datetime of when the http request completed
        /// </summary>
        public DateTime RequestCompleted { get; set; }

        /// <summary>
        /// A datetime of when the page content download started, this may be null if downloading the content was disallowed by the CrawlDecisionMaker or the inline delegate ShouldDownloadPageContent
        /// </summary>
        public DateTime? DownloadContentStarted { get; set; }

        /// <summary>
        /// A datetime of when the page content download completed, this may be null if downloading the content was disallowed by the CrawlDecisionMaker or the inline delegate ShouldDownloadPageContent
        /// </summary>
        public DateTime? DownloadContentCompleted { get; set; }

        /// <summary>
        /// The page that this pagee was redirected to
        /// </summary>
        public PageToCrawl RedirectedTo { get; set; }

        /// <summary>
        /// Time it took from RequestStarted to RequestCompleted in milliseconds
        /// </summary>
        public double Elapsed {
            get {
                return (RequestCompleted - RequestStarted).TotalMilliseconds;
            }
        }


        private HtmlDocument InitializeHtmlAgilityPackDocument()
        {
            HtmlDocument hapDoc = new HtmlDocument();
            hapDoc.OptionMaxNestedChildNodes = 5000;//did not make this an externally configurable property since it is really an internal issue to hap
            try
            {
                hapDoc.LoadHtml(Content.Text);
            }
            catch (Exception e)
            {
                hapDoc.LoadHtml("");

                _logger.ErrorFormat("Error occurred while loading HtmlAgilityPack object for Url [{0}]", Uri);
                _logger.Error(e);
            }
            return hapDoc;
        }

        private IHtmlDocument InitializeAngleSharpHtmlParser()
        {
            if(_angleSharpHtmlParser == null)
                _angleSharpHtmlParser = new HtmlParser();

            IHtmlDocument document;
            try
            {
                document = _angleSharpHtmlParser.Parse(Content.Text);
            }
            catch (Exception e)
            {
                document = _angleSharpHtmlParser.Parse("");

                _logger.ErrorFormat("Error occurred while loading AngularSharp object for Url [{0}]", Uri);
                _logger.Error(e);
            }

            return document;
        }
    }
}
