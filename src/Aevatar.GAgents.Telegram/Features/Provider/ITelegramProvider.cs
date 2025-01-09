using System.Threading.Tasks;
using AISmart.GAgent.Telegram.Dtos;

namespace AISmart.GAgent.Telegram.Provider;

public interface ITelegramProvider
{
    public Task<string> GetUpdatesAsync(string sendUser);
    
    public Task SendMessageAsync(string sendUser, string chatId, string message, ReplyParamDto? replyParam = null);
    public Task SendPhotoAsync(string sendUser, PhotoParamsRequest photoParams);
    public Task SetWebhookAsync(string sendUser, string webhook, string Token);
    Task DelWebhookAsync(string token);
}