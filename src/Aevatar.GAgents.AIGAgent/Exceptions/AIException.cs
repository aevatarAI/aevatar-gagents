using System;

namespace Aevatar.AI.Exceptions;

public class AIException : Exception
{
    public AIException(string message, Exception ex) : base(message, ex)
    {
    }
}