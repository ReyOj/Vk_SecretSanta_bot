using System.Net.Sockets;
using VkNet;
using VkNet.Model;
using System.Text;
using SantaBot.ApplicationContexts;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SantaBot.Extensions;
using SantaBot.Models;
using Serilog;
using VkNet.Exception;
using VkNet.Utils.BotsLongPool;
using User = SantaBot.Models.User;

var rnd = new Random();
var vkApi = new VkApi();
var db = new SqliteApplicationContext();

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

var logger = LoggerFactory.Create(x => x.AddSerilog(dispose: true)).CreateLogger<Program>();

if (!File.Exists("settings.json"))
{
    logger.LogCritical("Couldn't find settings.json file");
    return;
}

logger.LogInformation("Loading settings.json...");
var appSettings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText("settings.json"));
if (appSettings is null)
{
    logger.LogCritical("Settings object loaded from settings.json is null. Check this file");
    return;
}

logger.LogInformation("Authorizing...");
vkApi.Authorize(new ApiAuthParams
{
    AccessToken = appSettings.Token
});

var longPool = new BotsLongPoolUpdatesHandler(new BotsLongPoolUpdatesHandlerParams(vkApi, appSettings.GroupId)
{
    GetPause = () => false,
    
    OnException = ex =>
    {
        logger.LogError(ex, "Exception occurred that can cause malfunctions");
    },
    
    OnWarn = ex =>
    {
        switch (ex)
        {
            case PublicServerErrorException:
            case HttpRequestException:
            case SocketException:
                logger.LogWarning(ex, "VK servers are down");
                break;
            
            default:
                logger.LogWarning(ex, "Exception occured that not causing malfunctions");
                break;
        }
    },
    
    OnUpdates = OnUpdate
});

async void OnUpdate(BotsLongPoolOnUpdatesEvent e)
{
    var updates = new List<GroupUpdate>();

    foreach (var update in e.Updates)
    {
        if (update.Update != null)
        {
            updates.Add(update.Update);
            continue;
        }
            
        if (update.Exception == null)
            continue;
            
        logger.LogError("JSON serialization failed");
    }

    if (!updates.Any())
        return;

    var newMessages = updates.Where(x => x.Instance is MessageNew)
        .Select(x => x.Instance as MessageNew)
        .Select(x => x?.Message);

    foreach (var message in newMessages)
    {
        if (message is null)
            continue;

        var from = message.FromId;
        if (from is null)
            continue;
        
        logger.LogInformation("New message from {Id}: {Message}", message.PeerId, message.Text);

        if (await db.Users.AnyAsync(x => x.VkId == from && (x.Step == 1 || x.Step == 2)))
        {
            await Register(from, message.Text);
        }
        
        switch (message.Text.ToLower())
        {
            case "/start":
                await vkApi.SendMessageAsync(from, "Привет. По сути я бот для проведения всяких МП, но сейчас я в бета тесте. Ожидай новых сообщений!");
                break;
            
            case "/инфо":
                await vkApi.SendMessageAsync(from, "Создатель сказал, чтобы я сказал, что я нужен для какой-то дичи.\nКоманды:\n/запись — используется для участия\n/id — когда понадобится, ты сам об этом узнаешь\nПо всем вопросам пишите [https://vk.com/id531075153|Вовану]");
                break;
            
            case "пинг":
                await vkApi.SendMessageAsync(from, "понг");
                break;
            
            case "/запись":
                if (appSettings.RegistrationOpened)
                {
                    if (await db.Users.AnyAsync(x => x.VkId == from && x.Step >= 3))
                    {
                        await vkApi.SendMessageAsync(from, "Придержи коней, ты уже зарегистировался. Если нужно что-нибудь поправить — напиши /инфо и там будут контакты админа)");
                    }
                    else
                    {
                        var lastId = (await db.Users.OrderByDescending(x => x.Id).FirstAsync()).Id;
                        var usr1 = new User { Id = lastId + 1, VkId = from.Value, Name = "", Gift = "", Step = 0 };
                        await db.Users.AddAsync(usr1);
                        await db.SaveChangesAsync();
                        await Register(from, "");
                    }
                }
                else
                    await vkApi.SendMessageAsync(from, "Стоп. Регистрация закрыта. Если хочешь всё-таки принять участие — напиши /инфо и там будут контакты админа)");
                break;
            
            case "/стоп_запись":
                if (from == appSettings.AdminId)
                {
                    appSettings.RegistrationOpened = false;
                    File.WriteAllText("settings.json",
                        JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true }));
                    await vkApi.SendMessageAsync(appSettings.AdminId, "Регистрация успешно закрыта!");
                }
                else
                {
                    await vkApi.SendMessageAsync(from, "А кто это тут у нас решил побаловаться? Маме твоей я уже рассказал, жди выговора :)");
                }
                break;
            
            case "/старт_игры":
                if (from == appSettings.AdminId)
                {
                    await Game();
                    await vkApi.SendMessageAsync(appSettings.AdminId, "Успешно!");
                }
                else
                {
                    await vkApi.SendMessageAsync(appSettings.AdminId, "А кто это тут у нас решил побаловаться? Маме твоей я уже рассказал, жди выговора :)");
                }
                break;
            
            case "/id":
                if (!await db.Users.AnyAsync(x => x.VkId == from))
                    break;
                
                var user = await db.Users.FirstAsync(x => x.VkId == from);
                await vkApi.SendMessageAsync(from, "Твой ID в системе: " + user.Id + "\nИспользуется для упрощения жизни прогеру");
                break;
        }
    }
}

async Task Game()
{
    var users = await db.Users.OrderBy(x => x.Id).ToListAsync();
    List<User> shuffledUsers;
    
    do
    {
        shuffledUsers = users.OrderBy(_ => rnd.Next()).ToList();
    } while (users.Zip(shuffledUsers).Any(x => x.First.Id == x.Second.Id));

    for (var i = 0; i < users.Count; i++)
    {
        users[i].PointId = shuffledUsers[i].Id;
    }

    await db.SaveChangesAsync();
}

async Task Register(long? userId, string text)
{
    var user = await db.Users.FirstAsync(x => x.VkId == userId);
    
    switch(user.Step)
    {
        case 0:
            await vkApi.SendMessageAsync(userId, "Окей, напиши своё имя и фамилию. Только давай без выкрутасов, иначе модератор кикнет)");
            user.Step = 1;
            await db.SaveChangesAsync();
            break;
        
        case 1:
            user.Step = 2;
            user.Name = text;
            await db.SaveChangesAsync();
            
            await vkApi.SendMessageAsync(userId, "Записал, а теперь напиши пожелания к подарку");
            break;
        
        case 2:
            user.Step = 3;
            user.Gift = text;
            await vkApi.SendMessageAsync(userId, "Отлично! Жди дальнейших указаний)");
            await vkApi.SendMessageAsync(appSettings.AdminId, "&#10071;\nНовая запись!\nВК: vk.com/id" + userId + "\nИмя: "+user.Name+"\nПожелания: " + user.Gift);
            await db.SaveChangesAsync();
            
            break;
    }
}

await longPool.RunAsync();