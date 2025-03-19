
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.WebCrawler.GAgent.SEvents;
using Aevatar.GAgents.WebCrawler.GAgents.State;
using Orleans.Providers;

namespace Aevatar.GAgents.WebCrawler.GAgent;

[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
public class WebCrawlerGAgent : GAgentBase<WebCrawlerGAgentState, WebCrawlerSEvent>
{
    public override async Task<string> GetDescriptionAsync()
    {
        return "";
    }

    protected override void GAgentTransitionState(WebCrawlerGAgentState state, StateLogEventBase<WebCrawlerSEvent> @event)
    {
        switch (@event)
        {
            case SetWebUrlSEvent:
                break;
        }
    }
}

public interface IWebCrawlerGAgent : IStateGAgent<WebCrawlerGAgentState>
{
    Task 
}