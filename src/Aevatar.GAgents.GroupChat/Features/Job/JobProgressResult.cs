namespace Aevatar.GAgents.GroupChat.Abstract;

public class JobProgressResult<T>
{
    public bool IfContinue { get; set; }
    public T Result { get; set; }
}