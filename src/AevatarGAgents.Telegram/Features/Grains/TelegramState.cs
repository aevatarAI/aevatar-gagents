using System;
using Orleans;

namespace AISmart.GAgent.Telegram.Grains;
[GenerateSerializer]
public class TelegramState
{
    [Id(0)]  public  Guid Id { get; set; }
    [Id(1)] public string MessageId { get; set; }
    [Id(2)] public string SenderName{ get; set; }
    [Id(3)] public  string Message { get; set; }
}