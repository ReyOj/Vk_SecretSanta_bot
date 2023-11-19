using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    static ulong ts;
    static ulong? pts;
    static long oldId = 0;
    static long id = 0;
    static Random rnd = new Random();
    static private VkApi _api = new VkApi();
    static Vk_api vk = new Vk_api();
    static bool registration = true;
    static int AdminID = 531075153;//531075153
    static ApplicationCont db = new ApplicationCont();
    static bool reg = true;
    private static void Main(string[] args)
    {
        _api.Authorize(new ApiAuthParams()
        {
            AccessToken = ""
        });
        Console.WriteLine(_api.Token);

        LongPollServerResponse longPoolServerResponse = _api.Messages.GetLongPollServer(needPts: true);
        ts = Convert.ToUInt64(longPoolServerResponse.Ts);
        pts = longPoolServerResponse.Pts;
        while (true)
        {
            //Отправляем запрос на сервер
            LongPollHistoryResponse longPollResponse = _api.Messages.GetLongPollHistory(new MessagesGetLongPollHistoryParams()
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
                                    registration = true;
                                    var usr = db.Users.ToList();
                                    foreach(User u in usr)
                                    {
                                        if ((u.step == 2 || u.step == 1) && u.VkID == Convert.ToInt32(NowId))
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
                                            if (registration)
                                            {
                                                foreach (User u in usr)
                                                {
                                                    if (u.VkID == Convert.ToInt32(NowId) && u.step >= 3)
                                                    {
                                                        vk.SendMessage(NowId, "Придержи коней, ты уже зарегистировался. Если нужно что-нибудь поправить — напиши /инфо и там будут контакты админа)");
                                                        reg = false;
                                                    }
                                                }
                                                if (reg)
                                                {
                                                    var settings = db.Settings.Single();
                                                    User usr1 = new User { ID = settings.Count + 1, VkID = Convert.ToInt32(NowId), Name = "", Gift = "", step = 0 };
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
                                                if(u.VkID == Convert.ToInt32(NowId))
                                                {
                                                    vk.SendMessage(NowId, "Твой ID в системе: " + u.ID + "\nИспользуется для упрощения жизни прогеру");
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
    }
    async static void Game()
    {
        var set = db.Settings.Single();
        var us = db.Users.ToArray();
        for (int i = 1; i <= set.Count; i++)
        {
            bool flag = true;
            vk.SendMessage(Convert.ToInt64(us[i - 1].VkID), "Распределение началось!");
            int pointId = rnd.Next(1, set.Count);
            int co = 0;
            if (us[i - 1].ID == pointId)
            {
                while (pointId == i || us[i - 1].step == 4)
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
                        vk.SendMessage(us[i - 1].VkID, "У нас проблемы. Свяжись с админом");
                        break;
                    }
                }
            }
                if (flag)
                {
                    us[i - 1].step = 4;
                    us[i - 1].PoinId = pointId;
                    vk.SendMessage(us[i - 1].VkID, "Итак, данные твоей цели:\n[vk.com/id" + us[pointId - 1].VkID + "|" + us[pointId - 1].Name + "]\nПожелания: " + us[pointId - 1].Gift + "\nДействуй!");
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
            if(u.VkID == Userid)
            {
                step = u.step;
            }
        }
        switch(step)
        {
            case 0:
                vk.SendMessage(Userid, "Окей, напиши своё имя и фамилию. Только давай без выкрутасов, иначе модератор кикнет)");
                foreach (User u in usr)
                {
                    if (u.VkID == Userid)
                    {
                        u.step = 1;
                        db.SaveChanges();
                    }
                }
                break;
            case 1:
                foreach (User u in usr)
                {
                    if (u.VkID == Userid)
                    {
                        u.step = 2;
                        u.Name = text;
                        db.SaveChanges();
                    }
                }
                vk.SendMessage(Userid, "Записал, а теперь напиши пожелания к подарку");
                break;
            case 2:
                foreach (User u in usr)
                {
                    if (u.VkID == Userid)
                    {
                        u.step = 3;
                        u.Gift = text;
                        vk.SendMessage(Userid, "Отлично! Жди дальнейших указаний)");
                        vk.SendMessage(AdminID, "&#10071;\nНовая запись!\nВК: vk.com/id" + Userid + "\nИмя: "+u.Name+"\nПожелания: " + u.Gift);
                        db.SaveChanges();
                    }
                }
                break;
        }
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
            AccessToken = "vk1.a.lBTjAYq-lzIqShy3RsbaNdcR4zeb8qnpPX-xK7CFi-2YvWS-f_drNf2JGXPuQchgw_KTBXnFK_curB2VtQyRVWjSNzolBqyb6oh5qxCwI1wtrG09fxmlgW37IgWd5jRMp5qx_I4btmzbQ7YQSFcvJ66jwxSToWEWS7wGEK3LxlUdEUWSb4DIa8YWN7lQkBif5EChzgPeehDQsBkWGu2m1A"
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
public class User
{
    public int ID { get; set; }
    public int VkID { get; set; }
    public int step { get; set; }
    public string Name { get; set; }
    public string Gift { get; set; }
    public int PoinId { get; set; }
}
public class Settings
{
    public int ID { get; set; }
    public int Count { get; set; }
}
public class ApplicationCont : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Settings> Settings => Set<Settings>();
    public ApplicationCont() => Database.EnsureCreated();
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source = Users.db");
    }
}