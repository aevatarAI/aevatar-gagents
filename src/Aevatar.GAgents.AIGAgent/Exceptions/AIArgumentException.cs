using System;

namespace Aevatar.AI.Exceptions;

public class AIArgumentException : AIException
{
    public AIArgumentException(string message, Exception ex) : base(message, ex)
    {
    }

    public override AIExceptionEnum ExceptionEnum => AIExceptionEnum.ArgumentError;
}