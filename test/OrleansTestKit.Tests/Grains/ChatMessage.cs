namespace TestGrains;

[GenerateSerializer]
public class ChatMessage
{
    public ChatMessage(string msg)
    {
        Msg = msg;
    }

    [Id(0)] public string Msg { get; }
}
