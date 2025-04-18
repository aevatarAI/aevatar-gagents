
using System;

namespace Aevatar.AI.Exceptions;

public class AIArgumentNullException : AIException
{
    public AIArgumentNullException(string message, Exception ex) : base(message,ex)
    {
    }

    public override AIExceptionEnum ExceptionEnum => AIExceptionEnum.ArgumentNullError;
}
