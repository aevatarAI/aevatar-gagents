using System;

namespace Aevatar.AI.Exceptions;

public class AIRequestLimitException : AIException
{
    public AIRequestLimitException(string message, Exception ex) : base(message, ex)
    {
    }
}