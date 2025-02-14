namespace GroupChat.GAgent.Feature.Common;

[GenerateSerializer]
public class TalkResponse
{
    [Id(0)] public bool IfContinue { get; set; } = true;
    [Id(1)] public bool SkipSpeak { get; set; } = false;
    [Id(2)] public string SpeakContent { get; set; }
}

