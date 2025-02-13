namespace GroupChat.GAgent.Feature.Common;

[GenerateSerializer]
public class TalkResponse
{
    [Id(0)] public bool IfContinue { get; set; }
    [Id(1)] public string TalkContent { get; set; }
}

