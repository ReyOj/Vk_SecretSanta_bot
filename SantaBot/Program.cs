using System.Net.Sockets;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SantaBot.ApplicationContexts;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SantaBot.Models;
using Serilog;
using VkNet.Exception;
using VkNet.Utils.BotsLongPool;
using User = SantaBot.Models.User;

ulong ts;
ulong? pts;
long oldId = 0;
long id = 0;
var rnd = new Random();
var api = new VkApi();
Vk_api vk = new Vk_api();
//var AdminID = 531075153;//531075153
var db = new SqliteApplicationContext();
var reg = true;

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
api.Authorize(new ApiAuthParams
{
    AccessToken = appSettings.Token
});

var longPool = new BotsLongPoolUpdatesHandler(new BotsLongPoolUpdatesHandlerParams(api, appSettings.GroupId)
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

LongPollServerResponse longPoolServerResponse = api.Messages.GetLongPollServer(needPts: true);
ts = Convert.ToUInt64(longPoolServerResponse.Ts);
pts = longPoolServerResponse.Pts;
while (true)
{
    //Отправляем запрос на сервер
    LongPollHistoryResponse longPollResponse = api.Messages.GetLongPollHistory(new MessagesGetLongPollHistoryParams()
    {
        Ts = ts,
        Pts = pts
        //Fields = UsersFields.All //Указывает поля, которые будут возвращаться для каждого профиля. В данном примере для каждого отправителя сообщения получаем фото 100х100
    });

    //Получаем новый pts
    pts = longPollResponse.NewPts;
    //Console.WriteLine("+");
    Thread.Sleep(500);
    //Здесь пробегаемся по массиву событий
    for (int i = 0; i < longPollResponse.History.Count; i++)
    {
        //Console.Write(".");
        //И обрабатываем код события
        switch (longPollResponse.History[i][0])
        {
            //Код 4 - новое сообщение
            case 4:
                for(int j = 0; j<longPollResponse.Messages.Count; j++)
                {
                    long? NowId = longPollResponse.Messages[j].FromId;
                    string mess = longPollResponse.Messages[j].Text;
                    if (longPollResponse.History[i][1] == longPollResponse.Messages[j].Id && longPollResponse.Messages[j].Type == VkNet.Enums.MessageType.Received)
                    {
                        try
                        {
                            var usr = db.Users.ToList();
                            foreach(User u in usr)
                            {
                                if ((u.Step == 2 || u.Step == 1) && u.VkId == Convert.ToInt32(NowId))
                                {
                                    zap(NowId, mess);
                                }
                            }
                            switch (longPollResponse.Messages[j].Text.ToLower())
                            {
                                case "/start":
                                    vk.SendMessage(NowId, "Привет. По сути я бот для проведения всяких МП, но сейчас я в бета тесте. Ожидай новых сообщений!");
                                    break;
                                case "/инфо":
                                    vk.SendMessage(NowId, "Создатель сказал, чтобы я сказал, что я нужен для какой-то дичи.\nКоманды:\n/запись — используется для участия\n/id — когда понадобится, ты сам об этом узнаешь\nПо всем вопросам пишите [https://vk.com/id531075153|Вовану]");
                                    break;
                                case "пинг":
                                    vk.SendMessage(NowId, "понг");
                                    break;
                                case "/запись":
                                    if (appSettings.RegistrationOpened)
                                    {
                                        foreach (User u in usr)
                                        {
                                            if (u.VkId == Convert.ToInt32(NowId) && u.Step >= 3)
                                            {
                                                vk.SendMessage(NowId, "Придержи коней, ты уже зарегистировался. Если нужно что-нибудь поправить — напиши /инфо и там будут контакты админа)");
                                                reg = false;
                                            }
                                        }
                                        if (reg)
                                        {
                                            var settings = db.Settings.Single();
                                            User usr1 = new User { Id = settings.Count + 1, VkId = Convert.ToInt32(NowId), Name = "", Gift = "", Step = 0 };
                                            db.Users.Add(usr1);
                                            settings.Count += 1;
                                            db.SaveChanges();
                                            zap(NowId, "");
                                        }
                                    }
                                    else
                                    {
                                        vk.SendMessage(NowId, "Стоп. Регистрация закрыта. Если хочешь всё-таки принять участие — напиши /инфо и там будут контакты админа)");                                            }
                                    break;
                                case "/стоп_запись":
                                    if(NowId == AdminID)
                                    {
                                        registration = false;
                                        vk.SendMessage(AdminID, "Регистрация успешно закрыта!");
                                    }
                                    else
                                    {
                                        vk.SendMessage(NowId, "А кто это тут у нас решил побаловаться? Маме твоей я уже рассказал, жди выговора :)");
                                    }
                                    break;
                                case "/старт_игры":
                                    if (NowId == AdminID)
                                    {
                                        Game();
                                        vk.SendMessage(AdminID, "Успешно!");
                                    }
                                    else
                                    {
                                        vk.SendMessage(NowId, "А кто это тут у нас решил побаловаться? Маме твоей я уже рассказал, жди выговора :)");
                                    }
                                    break;
                                case "/id":
                                    foreach(User u in  usr) {
                                        if(u.VkId == Convert.ToInt32(NowId))
                                        {
                                            vk.SendMessage(NowId, "Твой ID в системе: " + u.Id + "\nИспользуется для упрощения жизни прогеру");
                                        }
                                    }
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
                break;
        }
    }
}

void OnUpdate(BotsLongPoolOnUpdatesEvent e)
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
        
    }
}

void Game()
{
    var set = db.Settings.Single();
    var us = db.Users.ToArray();
    for (int i = 1; i <= set.Count; i++)
    {
        bool flag = true;
        vk.SendMessage(Convert.ToInt64(us[i - 1].VkId), "Распределение началось!");
        int pointId = rnd.Next(1, set.Count);
        int co = 0;
        if (us[i - 1].Id == pointId)
        {
            while (pointId == i || us[i - 1].Step == 4)
            {
                pointId++;
                if (pointId == set.Count + 1)
                {
                    pointId = 1;
                    co++;
                }
                if (co > 1)
                {
                    flag = false;
                    vk.SendMessage(us[i - 1].VkId, "У нас проблемы. Свяжись с админом");
                    break;
                }
            }
        }
        if (flag)
        {
            us[i - 1].Step = 4;
            us[i - 1].PointId = pointId;
            vk.SendMessage(us[i - 1].VkId, "Итак, данные твоей цели:\n[vk.com/id" + us[pointId - 1].VkId + "|" + us[pointId - 1].Name + "]\nПожелания: " + us[pointId - 1].Gift + "\nДействуй!");
            db.SaveChanges();
        }
        Thread.Sleep(1000);
    }
}

    async static void zap(long? Userid, string text)
    {
        int step = 0;
        var usr = db.Users.ToList();
        foreach(User u in usr)
        {
            if(u.VkId == Userid)
            {
                step = u.Step;
            }
        }
        switch(step)
        {
            case 0:
                vk.SendMessage(Userid, "Окей, напиши своё имя и фамилию. Только давай без выкрутасов, иначе модератор кикнет)");
                foreach (User u in usr)
                {
                    if (u.VkId == Userid)
                    {
                        u.Step = 1;
                        db.SaveChanges();
                    }
                }
                break;
            case 1:
                foreach (User u in usr)
                {
                    if (u.VkId == Userid)
                    {
                        u.Step = 2;
                        u.Name = text;
                        db.SaveChanges();
                    }
                }
                vk.SendMessage(Userid, "Записал, а теперь напиши пожелания к подарку");
                break;
            case 2:
                foreach (User u in usr)
                {
                    if (u.VkId == Userid)
                    {
                        u.Step = 3;
                        u.Gift = text;
                        vk.SendMessage(Userid, "Отлично! Жди дальнейших указаний)");
                        vk.SendMessage(AdminID, "&#10071;\nНовая запись!\nВК: vk.com/id" + Userid + "\nИмя: "+u.Name+"\nПожелания: " + u.Gift);
                        db.SaveChanges();
                    }
                }
                break;
        }
    }

public class Vk_api
{
    static ulong ts;
    static ulong? pts;
    static long oldId = 0;
    static long id = 0;
    static Random rnd = new Random();
    static private VkApi _api = new VkApi();
    public Vk_api()
    {
        _api.Authorize(new ApiAuthParams()
        {
            AccessToken = ""
        });
        Console.WriteLine(_api.Token);
    }
    public void SendMessage(long? Userid, string message)
    {
        _api.Messages.Send(new MessagesSendParams
        {
            UserId = Userid,
            Message = message,
            RandomId = rnd.Next()
        });
    }
}
