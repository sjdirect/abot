using System;
using System.Collections.Generic;
using System.Net.Http;
using Abot2.Core;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Serilog;

namespace Abot2.Poco
{
    public class CrawledPage : PageToCrawl
    {
        /// <summary>
        /// Max node nesting depth kept in AngleSharpHtmlDocument, the same limit Chrome's html parser uses. Overly deep paths are flattened
        /// into siblings, keeping document order, so code that walks the document recursively, one call per level, can't overflow the stack.
        /// AngleSharp's own members no longer do so since 1.9.0; AngleSharp's parser limit (HtmlParserOptions.MaximumDepth) is lifted by
        /// AngleSharpHtmlParserFactory.DefaultOptions because it can put text out of source order.
        /// </summary>
        public const int MaxHtmlDocumentDepth = 512;

        /// <summary>
        /// Subtrees up to this height are moved whole when flattening, so small structures like links keep their content
        /// </summary>
        const int KeepIntactSubtreeHeight = 64;

        readonly IAngleSharpHtmlParserFactory _htmlParserFactory;

        readonly Lazy<IHtmlDocument> _angleSharpHtmlDocument;

        public CrawledPage(Uri uri)
            : this(uri, AngleSharpHtmlParserFactory.Default)
        {
        }

        /// <param name="uri">The url of the page</param>
        /// <param name="htmlParserFactory">Creates the parser for AngleSharpHtmlDocument when it is first read</param>
        public CrawledPage(Uri uri, IAngleSharpHtmlParserFactory htmlParserFactory)
            : base(uri)
        {
            _htmlParserFactory = htmlParserFactory ?? throw new ArgumentNullException(nameof(htmlParserFactory));
            _angleSharpHtmlDocument = new Lazy<IHtmlDocument>(InitializeAngleSharpHtmlParser);

            Content = new PageContent();
        }

        /// <summary>
        /// Lazy loaded AngleSharp IHtmlDocument (https://github.com/AngleSharp/AngleSharp) that can be used to retrieve/modify html elements on the crawled page.
        /// </summary>
        public virtual IHtmlDocument AngleSharpHtmlDocument => _angleSharpHtmlDocument.Value;

        /// <summary>
        /// Web request sent to the server.
        /// </summary>
        public HttpRequestMessage HttpRequestMessage { get; set; }

        /// <summary>
        /// Web response from the server.
        /// </summary>
        public HttpResponseMessage HttpResponseMessage { get; set; }

        /// <summary>
        /// The request exception that occurred during the request
        /// </summary>
        public HttpRequestException HttpRequestException { get; set; }

        /// <summary>
        /// The HttpClientHandler that was used to make the request to server
        /// </summary>
        public HttpClientHandler HttpClientHandler { get; set; }
        
        public override string ToString()
        {
            if(HttpResponseMessage == null)
                return Uri.AbsoluteUri;
         
            return $"{Uri.AbsoluteUri}[{Convert.ToInt32(HttpResponseMessage.StatusCode)}]";
        }

        /// <summary>
        /// Links parsed from page. This value is set by the WebCrawler.SchedulePageLinks() method only If the "ShouldCrawlPageLinks" rules return true or if the IsForcedLinkParsingEnabled config value is set to true.
        /// </summary>
        public IEnumerable<HyperLink> ParsedLinks { get; set; }

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
        public double Elapsed => (RequestCompleted - RequestStarted).TotalMilliseconds;


        private IHtmlDocument InitializeAngleSharpHtmlParser()
        {
            var angleSharpHtmlParser = _htmlParserFactory.Create();

            IHtmlDocument document;
            try
            {
                document = angleSharpHtmlParser.ParseDocument(Content.Text);
                FlattenNodesDeeperThan(document, MaxHtmlDocumentDepth);
            }
            catch (Exception e)
            {
                document = angleSharpHtmlParser.ParseDocument("");

                Log.Error("Error occurred while loading AngularSharp object for Url [{0}] {@Exception}", Uri, e);
            }

            return document;
        }

        /// <summary>
        /// Iteratively walks the tree and flattens the descendants of any node at depth (maxDepth - KeepIntactSubtreeHeight - 1)
        /// so that no node ends up deeper than maxDepth
        /// </summary>
        private static void FlattenNodesDeeperThan(INode root, int maxDepth)
        {
            var flattenDepth = maxDepth - KeepIntactSubtreeHeight - 1;
            var pending = new Stack<(INode Node, int Depth)>();
            pending.Push((root, 0));
            while (pending.Count > 0)
            {
                var (node, depth) = pending.Pop();
                if (depth == flattenDepth)
                {
                    FlattenDescendants(node);
                    continue;
                }

                foreach (var child in node.ChildNodes)
                    pending.Push((child, depth + 1));
            }
        }

        /// <summary>
        /// Re-parents node's descendants as its direct children in document order. Subtrees no taller than KeepIntactSubtreeHeight
        /// are moved whole (so an anchor keeps its text), only the nodes on overly deep paths are emptied and lifted up
        /// </summary>
        private static void FlattenDescendants(INode node)
        {
            var descendants = new List<INode>();
            var pending = new Stack<INode>();
            for (var i = node.ChildNodes.Length - 1; i >= 0; i--)
                pending.Push(node.ChildNodes[i]);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                descendants.Add(current);
                for (var i = current.ChildNodes.Length - 1; i >= 0; i--)
                    pending.Push(current.ChildNodes[i]);
            }

            //Reverse document order visits children before their parent
            var heights = new Dictionary<INode, int>();
            var tallestChild = 0;
            for (var i = descendants.Count - 1; i >= 0; i--)
            {
                var height = 0;
                foreach (var child in descendants[i].ChildNodes)
                    height = Math.Max(height, heights[child] + 1);

                heights[descendants[i]] = height;
                if (descendants[i].Parent == node)
                    tallestChild = Math.Max(tallestChild, height);
            }

            if (tallestChild <= KeepIntactSubtreeHeight)
                return;

            //Short subtrees are kept whole, tall nodes are lifted out of their subtree and their children processed in turn
            var lifted = new List<INode>();
            for (var i = node.ChildNodes.Length - 1; i >= 0; i--)
                pending.Push(node.ChildNodes[i]);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                lifted.Add(current);
                if (heights[current] <= KeepIntactSubtreeHeight)
                    continue;

                for (var i = current.ChildNodes.Length - 1; i >= 0; i--)
                    pending.Push(current.ChildNodes[i]);
            }

            //Detach in reverse so a tall node has already lost its children when it is removed, which keeps AngleSharp from walking a deep subtree
            for (var i = lifted.Count - 1; i >= 0; i--)
                lifted[i].Parent.RemoveChild(lifted[i]);

            foreach (var liftedNode in lifted)
                node.AppendChild(liftedNode);
        }
    }
}
