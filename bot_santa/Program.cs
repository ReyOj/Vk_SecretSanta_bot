using VkNet;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;

internal class Program
{
    static ulong ts;
    static ulong? pts;
    static long oldId = 0;
    static long id = 0;
    static Random rnd = new Random();
    static INIManager ini = new INIManager();
    static private VkApi _api = new VkApi();
    static Vk_api vk = new Vk_api();
    private static void Main(string[] args)
    {
        _api.Authorize(new ApiAuthParams()
        {
            AccessToken = "vk1.a.lBTjAYq-lzIqShy3RsbaNdcR4zeb8qnpPX-xK7CFi-2YvWS-f_drNf2JGXPuQchgw_KTBXnFK_curB2VtQyRVWjSNzolBqyb6oh5qxCwI1wtrG09fxmlgW37IgWd5jRMp5qx_I4btmzbQ7YQSFcvJ66jwxSToWEWS7wGEK3LxlUdEUWSb4DIa8YWN7lQkBif5EChzgPeehDQsBkWGu2m1A"
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
                            if (longPollResponse.History[i][1] == longPollResponse.Messages[j].Id && longPollResponse.Messages[j].Type == VkNet.Enums.MessageType.Received)
                            {
                                try
                                {
                                    if (ini.Get(longPollResponse.Messages[j].FromId.ToString(), "Step") == "2" || ini.Get(longPollResponse.Messages[j].FromId.ToString(), "Step") == "1")
                                    {
                                        zap(longPollResponse.Messages[j].FromId, longPollResponse.Messages[j].Text);
                                    }
                                    switch (longPollResponse.Messages[j].Text)
                                    {
                                        case "/start":
                                            vk.SendMessage(longPollResponse.Messages[j].FromId, "Привет. По сути я бот для проведения всяких МП, но сейчас я в бета тесте. Ожидай новых сообщений!");
                                            break;
                                        case "/инфо":
                                            vk.SendMessage(longPollResponse.Messages[j].FromId, "Создатель сказал, чтобы я сказал, что я нужен для какой-то дичи. По всем вопросам пишите [https://vk.com/id531075153|Вовану]");
                                            break;
                                        case "пинг":
                                            vk.SendMessage(longPollResponse.Messages[j].FromId, "понг");
                                            break;
                                        case "/запись":
                                            ini.Write(longPollResponse.Messages[j].FromId.ToString(), "Step", "0");
                                            zap(longPollResponse.Messages[j].FromId, "");
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

    async static void zap(long? Userid, string text)
    {
        switch(ini.Get(Userid.ToString(), "Step"))
        {
            case "0":
                vk.SendMessage(Userid, "Окей, напиши своё имя и фамилию. Только давай без выкрутасов, иначе модератор кикнет)");
                ini.Write(Userid.ToString(), "Step", "1");
                break;
            case "1":
                ini.Write(Userid.ToString(), "Name", text);
                vk.SendMessage(Userid, "Записал, а теперь напиши пожелания к подарку");
                ini.Write(Userid.ToString(), "Step", "2");
                break;
            case "2":
                ini.Write(Userid.ToString(), "Gift", text);
                vk.SendMessage(Userid, "Отлично! Жди дальнейших указаний)");
                vk.SendMessage(531075153, "&#10071;\nНовая запись!\nВК: vk.com/id" + Userid+"\nИмя: "+ini.Get(Userid.ToString(), "Name")+"\nПожелания: "+ini.Get(Userid.ToString(), "Gift"));
                ini.Write(Userid.ToString(), "Step", "3");
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
    static INIManager ini = new INIManager();
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
public class INIManager
{
    //Конструктор, принимающий путь к INI-файлу
    public INIManager(string aPath)
    {
        path = "C:/test/users.ini";
    }

    //Конструктор без аргументов (путь к INI-файлу нужно будет задать отдельно)
    public INIManager() : this("") { }

    //Возвращает значение из INI-файла (по указанным секции и ключу) 
    public string Get(string aSection, string aKey)
    {
        //Для получения значения
        StringBuilder buffer = new StringBuilder(SIZE);

        //Получить значение в buffer
        GetPrivateString(aSection, aKey, null, buffer, SIZE, path);

        //Вернуть полученное значение
        return buffer.ToString();
    }

    //Пишет значение в INI-файл (по указанным секции и ключу) 
    public void Write(string aSection, string aKey, string aValue)
    {
        //Записать значение в INI-файл
        WritePrivateString(aSection, aKey, aValue, path);
    }

    //Возвращает или устанавливает путь к INI файлу
    public string Path { get { return path; } set { path = value; } }

    //Поля класса
    private const int SIZE = 1024; //Максимальный размер (для чтения значения из файла)
    private string path = null; //Для хранения пути к INI-файлу

    //Импорт функции GetPrivateProfileString (для чтения значений) из библиотеки kernel32.dll
    [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString")]
    private static extern int GetPrivateString(string section, string key, string def, StringBuilder buffer, int size, string path);

    //Импорт функции WritePrivateProfileString (для записи значений) из библиотеки kernel32.dll
    [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileString")]
    private static extern int WritePrivateString(string section, string key, string str, string path);
}