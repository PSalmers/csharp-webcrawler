using System;
using System.Runtime.CompilerServices;
using Abot.Crawler;
using Abot.Poco;
using Abot.Core;
using HtmlAgilityPack;
using AngleSharp;

namespace FinalCrawlerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            PoliteWebCrawler crawler = CreateCrawler(Convert.ToInt32(args[1]), Convert.ToInt32(args[2]));
            crawler.CrawlBag.Count = 0;
            CrawledLinks crawledLinks = new CrawledLinks($"Links from {args[0]}");
            crawler.CrawlBag.CrawledLinks = crawledLinks;
            CrawlFromSeed(args[0], crawler);
            crawledLinks.GenerateReport();
            Console.WriteLine("Report Successfully generated, press any key to quit");
            Console.ReadKey();
        }

        private static PoliteWebCrawler CreateCrawler(int recursionDepth, int maxLinks)
        {
            CrawlConfiguration crawlConfig = AbotConfigurationSectionHandler.LoadFromXml().Convert();
            crawlConfig.MaxCrawlDepth = recursionDepth;
            crawlConfig.MaxConcurrentThreads = 20;
            crawlConfig.MaxLinksPerPage = maxLinks;
            //Must pass an instance of AngleSharpHyperlinkParser to override default customized HAP parser, which is incompatible with my installed HAP dll
            PoliteWebCrawler crawler = new PoliteWebCrawler(crawlConfig, null, null, null, null, new AngleSharpHyperlinkParser(), null, null, null);
            crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
            crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
            return crawler;
        }

        private static void CrawlFromSeed(string url, PoliteWebCrawler crawler)
        {
            Console.WriteLine("Crawling from seed");
            CrawlResult result = crawler.Crawl(new Uri(url));
            if (result.ErrorOccurred)
                Console.WriteLine("Crawl of {0} completed with error: {1}", result.RootUri.AbsoluteUri, result.ErrorException.Message);
            else
                Console.WriteLine("Crawl of {0} completed without error.", result.RootUri.AbsoluteUri);
        }

        static void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            string childUrl = e.PageToCrawl.Uri.AbsoluteUri;
            string parentUrl = e.PageToCrawl.ParentUri.AbsoluteUri;
            CrawlContext context = e.CrawlContext;
            CrawledLinks crawledLinks = context.CrawlBag.CrawledLinks;
            crawledLinks.AddRelation(parentUrl, childUrl);
        }

        static void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            Console.WriteLine($"Crawled {crawledPage.Uri.AbsoluteUri}");
            e.CrawlContext.CrawlBag.Count++;
            Console.WriteLine($"Total pages crawled: {e.CrawlContext.CrawlBag.Count}");
        }

        
    }

    class CrawledLinks
    {
        private HtmlDocument htmlDocument;
        private HtmlNode rootList;
        private HtmlNodeCollection lastParentNodeCollection;
        private string lastParentUrl;

        public CrawledLinks(string title)
        {
            htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml("<!DOCTYPE html><html><body></body></html>");
            HtmlNode bodyNode = htmlDocument.DocumentNode.SelectSingleNode("//body");
            bodyNode.AppendChild(HtmlNode.CreateNode($"<h1>{title}</h1>"));
            rootList = HtmlNode.CreateNode("<ul></ul>");
            bodyNode.AppendChild(rootList);
        }

        private HtmlNode CreateListItem(string url)
        {
            HtmlNode listItem = HtmlNode.CreateNode("<li></li>");
            HtmlNode nestedListNode = HtmlNode.CreateNode("<ul></ul>");
            HtmlNode details = HtmlNode.CreateNode("<details></details>");
            details.AppendChild(HtmlNode.CreateNode($"<summary>{url}</summary>"));
            details.AppendChild(nestedListNode);
            listItem.AppendChild(details);
            return listItem;
        }
        
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AddRelation(string parentUrl, string childUrl)
        {
            HtmlNode listItem = CreateListItem(childUrl);

            if (lastParentUrl == parentUrl)
            {
                AppendChildToLists(listItem, lastParentNodeCollection);
            } else
            {
                HtmlNodeCollection parentUrlLists = htmlDocument.DocumentNode.SelectNodes($"//li/details[summary = \"{parentUrl}\"]/ul");
                lastParentNodeCollection = parentUrlLists;
                AppendChildToLists(listItem, parentUrlLists);
            }

            lastParentUrl = parentUrl;
        }

        private void AppendChildToLists(HtmlNode listItem, HtmlNodeCollection parentUrlLists)
        {
            if (parentUrlLists != null)
                foreach (HtmlNode list in parentUrlLists)
                    list.AppendChild(listItem);
            else
                rootList.AppendChild(listItem);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void GenerateReport()
        {
            htmlDocument.Save("./link_report.html");
        }
    }
}
