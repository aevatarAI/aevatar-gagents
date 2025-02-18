using Aevatar.Core.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aevatar.GAgents.Basic.Abstractions;

public interface IHttpHandlerGAgent : IGAgent
{
    Task<IActionResult> HandleRequestAsync(HttpRequest request);
}