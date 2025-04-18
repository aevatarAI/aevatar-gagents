using System;
using System.Net;

namespace Aevatar.AI.Exceptions;

public class AIHttpOperationException : AIException
{
    public HttpStatusCode? State { get; }
    public string? ResponseContent { get; }

    public AIHttpOperationException(HttpStatusCode? state, string? responseContent, string message, Exception ex) :
        base(message, ex)
    {
        State = state;
        ResponseContent = responseContent;
    }

    public override AIExceptionEnum ExceptionEnum => AIExceptionEnum.HttpOperationError;
}