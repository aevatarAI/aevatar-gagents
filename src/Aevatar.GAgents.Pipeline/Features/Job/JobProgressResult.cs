namespace Aevatar.GAgents.Pipeline.Abstract;

public class JobProgressResult<T>
{
    public bool IfContinue { get; set; } = true;
    public T Result { get; set; }
}