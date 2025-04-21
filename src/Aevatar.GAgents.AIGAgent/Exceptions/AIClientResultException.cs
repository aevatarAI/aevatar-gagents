using System;

namespace Aevatar.AI.Exceptions;

public class AIClientResultException:AIException
{
    public AIClientResultException(string message, Exception ex) : base(message, ex)
    {
    }

    public override AIExceptionEnum ExceptionEnum { get; }
}