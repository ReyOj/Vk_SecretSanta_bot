using VkNet.Abstractions;
using VkNet.Model;

namespace SantaBot.Extensions;

public static class VkApiExtensions
{
    public static void SendMessage(this IVkApi api,
        long? userId, string message)
    {
        api.Messages.Send(new MessagesSendParams
        {
            UserId = userId,
            Message = message,
            RandomId = Random.Shared.Next()
        });
    }
    
    public static Task SendMessageAsync(this IVkApi api, 
        long? userId, string message)
    {
       return api.Messages.SendAsync(new MessagesSendParams
       {
           UserId = userId,
           Message = message,
           RandomId = Random.Shared.Next()
       });
    }
}