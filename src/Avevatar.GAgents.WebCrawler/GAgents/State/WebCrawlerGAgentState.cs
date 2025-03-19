using Aevatar.Core.Abstractions;

namespace Aevatar.GAgents.WebCrawler.GAgents.State;

[GenerateSerializer]
public class WebCrawlerGAgentState : StateBase
{
    [Id(0)] public Guid Id { get; set; } = Guid.NewGuid();
    [Id(1)] public string UserId { get; set; }
    [Id(2)] public string Url { get; set; }
    [Id(3)] public string CrawlResult { get; set; }
    [Id(4)] public List<string> CrawlRecords { get; set; }
    [Id(3)] public string Cron { get; set; }
}