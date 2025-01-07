using System.Threading.Tasks;
using Orleans;

namespace AISmart.GAgent.Telegram.Grains;

public interface ITelegramGrain:IGrainWithStringKey
{
    public Task SendMessageAsync(string sendUser, string chatId, string message,string? replyMessageId);
    public Task RegisterTelegramAsync(string sendUser, string token);

    Task UnRegisterTelegramAsync(string stateBotName);
}