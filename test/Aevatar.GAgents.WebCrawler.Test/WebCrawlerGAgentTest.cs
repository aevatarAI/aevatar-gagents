using Abot2.Core;
using Abot2.Crawler;
using Abot2.Poco;
using Serilog;
using Xunit;

namespace Aevatar.GAgents.WebCrawler.Test;

public class WebCrawlerGAgentTest : AevatarWebCrawlerTestBase
{
    [Fact]
    public async Task AbotTest()
    {
        await DemoSimpleCrawler();
        await DemoSinglePageRequest();
    }
    
    private static async Task DemoSimpleCrawler()
    {
        var config = new CrawlConfiguration
        {
            MaxPagesToCrawl = 10, //Only crawl 10 pages
            MinCrawlDelayPerDomainMilliSeconds = 3000 //Wait this many millisecs between requests
        };
        var crawler = new PoliteWebCrawler(config);

        crawler.PageCrawlCompleted += PageCrawlCompleted;//Several events available...

        var crawlResult = await crawler.CrawlAsync(new Uri("https://github.com/aevatarAI/aevatar-samples/branches"));
    }

    private static async Task DemoSinglePageRequest()
    {
        var pageRequester = new PageRequester(new CrawlConfiguration(), new WebContentExtractor());

        var crawledPage = await pageRequester.MakeRequestAsync(new Uri("https://github.com/aevatarAI/aevatar-samples/branches"));
        Log.Logger.Information("{result}", new
        {
            url = crawledPage.Uri,
            status = Convert.ToInt32(crawledPage.HttpResponseMessage.StatusCode)
        });
    }

    private static void PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
    {
        var httpStatus = e.CrawledPage.HttpResponseMessage.StatusCode;
        var rawPageText = e.CrawledPage.Content.Text;
    }
}