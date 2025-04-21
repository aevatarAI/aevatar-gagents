using System;

namespace Aevatar.AI.Exceptions;

public class AIOtherException : AIException
{
    public AIOtherException(string message, Exception ex) : base(message, ex)
    {
    }

    public override AIExceptionEnum ExceptionEnum => AIExceptionEnum.OtherException;
}