using System;
using OpenQA.Selenium;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using System.Threading;
using OpenQA.Selenium.Interactions;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using Telegram.Bot.Types;
using File = System.IO.File;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Args;
using DevLib.Timers;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Threading.Tasks;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium.Chrome;
using OfficeOpenXml;

namespace ConsoleApp5
{
    class Program
    {
        private static Dictionary<int, UserDialogInfo> users = new Dictionary<int, UserDialogInfo>();
        private static string about = @"🌈Enjoy your colour🌈

👥Реклама, сотрудничество, ВП:
— @ColourBooomSekretarBot

👇Писать отзывы сюда:
— @mirtrudmonnalisa (Лиза)

💸На развитие бота:
— https://www.donationalerts.com/r/colourbooom100

by MirTrudMay®";
        private static string welcome = @"Салют, @имя человека! Я могу быстро и легко окрасить твои волосы. Не веришь? Тогда скорее выбирай цвет, присылай мне своё фото и узри магию🧚🏻‍♂️ ";
        private static string afterColorChoice = @"Отличный выбор! Теперь фото👩🏼";
        private static string afterResult = @"Ну как вам? Будем рады отзыву о боте, они нам очень помогают☺️ Написать отзыв можно сюда: @mirtrudmonnalisa (Лиза)";
        private static readonly TelegramBotClient Bot = new TelegramBotClient("1122140217:AAGO_AVkDhXu8OdWu-6vPniMKdJ5GdzLEdU");
        private static ReplyKeyboardMarkup replyKeyboard = null;
        private static List<Advert> adverts = new List<Advert>();
        private static Advert currentAdvert;
        private static AdminStep? step = null;
        private static List<Message> createMessages = new List<Message>();
        private static bool isAdmined = false;
        private static Dictionary<Advert, ThreadTimer> timers = new Dictionary<Advert, ThreadTimer>();
        private static Dictionary<int,string> usersIds = new Dictionary<int, string>();
        private static object lockObj = new object();
        private static HashSet<int> processingUsers = new HashSet<int>();
        private static InlineKeyboardButton[] cancelAdminButton = new[] { InlineKeyboardButton.WithCallbackData("Отмена", "admin cancel") };
        private static InlineKeyboardButton[] cancelButton = new[] { InlineKeyboardButton.WithCallbackData("Отмена", "cancel") };
        private static Dictionary<string, string> colors;

        private static void Main()
        {
            ExcelPackage.LicenseContext = LicenseContext.Commercial;
            Initialize();
            RunBotAsync().GetAwaiter().GetResult();
        }

        private static void Initialize()
        {
            colors = new Dictionary<string, string>
            {
                {"copper gold", "Extra Light Blonde Copper Gold"},
                {"blonde warm neutral", "Medium Blonde Warm Neutral"},
                {"blonde ash", "Extra Light Blonde Ash"},
                {"blonde gold", "Extra Light Blonde Gold"},
                {"blonde neutral", "Extra Light Blonde Neutral"},
                {"brown neutral", "Light Brown Neutral"},
                {"brown gold", "Medium Brown Gold"},
                {"light brown", "Light Brown Gold"},
                {"brown warm neutral", "Medium Brown Warm Neutral"},
                {"brown", "Medium Brown Gold"},
                {"brown red", "Light Brown Red Red"},
                {"red violet", "Light Brown Red Violet Plus"},
                {"red copper", "Light Brown Red Copper Plus"}
            };
            var colours = new[] { "lucky duck yellow", "flamenco fuschia", "orange alert", "admiral navy", "retro blue", "mermaid teal", "royal purple", "blooming orchid", "clover green", "red hot", "sparkling rose", "bubblegum pink", "lavender macaron", "sweet mint", "stonewashed denim"};
            List<List<KeyboardButton>> buttons = new List<List<KeyboardButton>> { new List<KeyboardButton>() { new KeyboardButton("палитра цветов") } };
            foreach (var color in colours)
            {
                if (buttons.Count <= 1 || buttons[^1].Count >= 2)
                    buttons.Add(new List<KeyboardButton>());
                buttons[^1].Add(new KeyboardButton(color));
            }
            foreach (var color in colors.Keys)
            {
                if (buttons.Count <= 1 || buttons[^1].Count >= 2)
                    buttons.Add(new List<KeyboardButton>());
                buttons[^1].Add(new KeyboardButton(color));
            }
            foreach (var colour in colours)
            {
                colors.Add(colour, colour);
            }
            colours = new[] { "black", "marble gray" };
            foreach (var color in colours)
            {
                if (buttons.Count <= 1 || buttons[^1].Count >= 2)
                    buttons.Add(new List<KeyboardButton>());
                buttons[^1].Add(new KeyboardButton(color));
            }
            replyKeyboard = new ReplyKeyboardMarkup(buttons);
            foreach (var colour in colours)
            {
                colors.Add(colour, colour);
            }
        }

        private static async Task RunBotAsync()
        {
            try
            {
                if (File.Exists("users"))
                {
                    using (var file = File.OpenRead("users"))
                    {
                        usersIds = (Dictionary<int,string>)new BinaryFormatter().Deserialize(file);
                    }
                }
            }
            catch (Exception ex)
            { }
            try
            {
                if (File.Exists("adverts"))
                {
                    using (var file = File.OpenRead("adverts"))
                    {
                        adverts = (List<Advert>)new BinaryFormatter().Deserialize(file);
                        foreach (var ad in adverts)
                        {
                            ThreadTimer timer = new ThreadTimer(ad.Period.Value.TotalMilliseconds, DateTimeOffset.Now.Add(ad.Period.Value));
                            timer.Tag = ad;
                            timer.Elapsed += Program_Elapsed;
                            timers.TryAdd(ad, timer);
                            if (ad.IsActive)
                            {
                                timer.Start();
                            }
                            else
                            {
                                timer.Stop();
                            }
                        }
                    }
                }
            }
            catch { }
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            Bot.SetMyCommandsAsync(new BotCommand[] { new BotCommand() { Command = "start", Description = "Запустить бота" } }).GetAwaiter().GetResult();
            Bot.SetMyCommandsAsync(new BotCommand[] { new BotCommand() { Command = "about", Description = "Информация о боте" } }).GetAwaiter().GetResult();
            Bot.OnMessage += Bot_OnMessage;
            Bot.OnCallbackQuery += Bot_OnCallbackQuery;
            Bot.StartReceiving();
            Bot.StartReceiving();
            Console.WriteLine("start");
            await Task.Delay(-1);
        }

        private static async void Bot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            if (isAdmined && IsAdmin(e.CallbackQuery.From.Id))
            {
                try
                {
                    string callback = e.CallbackQuery.Data;
                    if (callback.StartsWith("change state $"))
                    {
                        callback = callback.Substring("change state $".Length);
                        foreach (var ad in adverts)
                        {
                            if (ad.Name == callback)
                            {
                                await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
                                ad.IsActive = !ad.IsActive;
                                if (!ad.IsActive)
                                {
                                    timers[ad].Stop();
                                }
                                else
                                {
                                    if (ad.Count >= ad.MaxCount || ad.MaxCount < 0)
                                    {
                                        ad.Count = 0;
                                    }
                                    timers[ad].Start();
                                }
                                string text;
                                if(ad.IsActive)
                                {
                                    text = "Остановить";
                                }
                                else
                                {
                                    if(ad.Count>=ad.MaxCount || ad.MaxCount < 0)
                                    {
                                        text = "Запустить";
                                    }
                                    else
                                    {
                                        text = "Возобновить";
                                    }
                                }
                                InlineKeyboardButton[][] buttons = new[]
                                {
                                        new[] { InlineKeyboardButton.WithCallbackData("Сообщение", "message "+"$"+ad.Name), InlineKeyboardButton.WithCallbackData("Период рассылки", "period " + "$" + ad.Name) },
                                        new[] { InlineKeyboardButton.WithCallbackData("Количество рассылок", "count " + "$" + ad.Name), InlineKeyboardButton.WithCallbackData(text, "change state "+"$"+ad.Name)},
                                        new[]{ InlineKeyboardButton.WithCallbackData("Удалить", "delete " + "$" + ad.Name),InlineKeyboardButton.WithCallbackData("Назад", "all adverts") }
                                };
                                await Bot.EditMessageReplyMarkupAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, replyMarkup: new InlineKeyboardMarkup(buttons));
                                break;
                            }
                        }
                        return;
                    }
                    if (callback.StartsWith("count $"))
                    {
                        callback = callback.Substring("count $".Length);
                        foreach (var ad in adverts)
                        {
                            if (ad.Name == callback)
                            {
                                await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
                                try
                                {
                                    List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>()
                                    {
                                        new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Изменить","change count $"+ad.Name),InlineKeyboardButton.WithCallbackData("Назад","$"+ad.Name) }
                                    };
                                    InlineKeyboardMarkup keyboardMarkups = new InlineKeyboardMarkup(buttons);
                                    string count="Количество рассылок: ";
                                    if(ad.MaxCount<0)
                                    {
                                        count += "∞";
                                    }
                                    else
                                    {
                                        count += ad.Count + "/" + ad.MaxCount;
                                    }
                                    await Bot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId, count, replyMarkup: keyboardMarkups);
                                }
                                catch { }
                                break;
                            }
                        }
                        return;
                    }
                    if (callback.StartsWith("change count $"))
                    {
                        callback = callback.Substring("change count $".Length);
                        foreach (var ad in adverts)
                        {
                            if (ad.Name == callback)
                            {
                                await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
                                try
                                {
                                    createMessages.Add(Bot.SendTextMessageAsync(e.CallbackQuery.From.Id,"Пришлите мне новое количество рассылок записи", replyMarkup: new InlineKeyboardMarkup(cancelAdminButton)).Result);
                                    step = AdminStep.Count;
                                    currentAdvert = ad;
                                }
                                catch { }
                                break;
                            }
                        }
                        return;
                    }
                        if (callback.StartsWith("delete $"))
                    {
                        callback = callback.Substring("delete $".Length);
                        foreach (var ad in adverts)
                        {
                            if (ad.Name == callback)
                            {
                                await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
                                timers[ad].Dispose();
                                timers.Remove(ad);
                                adverts.Remove(ad);
                                SerializeAdverts();
                                try
                                {
                                    List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>()
                                    {
                                        new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Добавить рекламную запись","add advert") }
                                    };
                                    if (adverts.Count > 0)
                                        buttons.Insert(0, new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Посмотреть рекламные записи", "all adverts") });
                                    buttons.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Посмотреть юзеров", "get users") });
                                    buttons.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Выйти", "exit") });
                                    InlineKeyboardMarkup keyboardMarkups = new InlineKeyboardMarkup(buttons);
                                    await Bot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId, "Что вы хотите сделать?", replyMarkup: keyboardMarkups);
                                }
                                catch { }
                                break;
                            }
                        }
                        return;
                    }
                    if (callback.StartsWith("message $"))
                    {
                        callback = callback.Substring("message $".Length);
                        foreach (var ad in adverts)
                        {
                            if (ad.Name == callback)
                            {
                                await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
                                try
                                {
                                    string message = "Сообщение записи " + ad.Name;
                                    InlineKeyboardButton[] keyboard = new[] { InlineKeyboardButton.WithCallbackData("Изменить", "change message " + "$" + ad.Name), InlineKeyboardButton.WithCallbackData("Назад", "$" + ad.Name) };
                                    if (currentAdvert.Message.FileId == null)
                                    {
                                        await Bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, message + ":\n" +ad.Message.Text, replyMarkup: new InlineKeyboardMarkup(keyboard));
                                    }
                                    else
                                    {
                                        await Bot.DeleteMessageAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                                        if (currentAdvert.Message.Text == null)
                                        {
                                            await Bot.SendPhotoAsync(e.CallbackQuery.Message.Chat, new InputOnlineFile(currentAdvert.Message.FileId), caption: message, replyMarkup: new InlineKeyboardMarkup(keyboard));
                                        }
                                        else
                                        {
                                            await Bot.SendPhotoAsync(e.CallbackQuery.Message.Chat, new InputOnlineFile(currentAdvert.Message.FileId), caption: message + ":\n" + ad.Message.Text, replyMarkup: new InlineKeyboardMarkup(keyboard));
                                        }
                                    }
                                }
                                catch { }
                                break;
                            }
                        }
                        return;
                    }
                    if (callback.StartsWith("period $"))
                    {
                        callback = callback.Substring("period $".Length);
                        foreach (var ad in adverts)
                        {
                            if (ad.Name == callback)
                            {
                                await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
                                try
                                {
                                    string message = "Период отправки сообщения " + ad.Name + ":\n";
                                    message += CreateTimeString(ad.Period.Value);
                                    InlineKeyboardButton[] keyboard = new[] { InlineKeyboardButton.WithCallbackData("Изменить", "change period $"+ad.Name), InlineKeyboardButton.WithCallbackData("Назад", "$" + ad.Name) };
                                    await Bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, message, replyMarkup: new InlineKeyboardMarkup(keyboard));
                                }
                                catch { }
                                break;
                            }
                        }
                        return;
                    }
                    if (callback.StartsWith("change message $"))
                    {
                        callback = callback.Substring("change message $".Length);
                        foreach (var ad in adverts)
                        {
                            if (ad.Name == callback)
                            {
                                await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
                                try
                                {
                                    step = AdminStep.Message;
                                    currentAdvert = ad;
                                    createMessages.Add(Bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat, "Пришлите мне новое сообщение для рассылки (текст и картинка)", replyMarkup: new InlineKeyboardMarkup(cancelAdminButton)).Result);
                                }
                                catch { }
                                break;
                            }
                        }
                        return;
                    }
                    if (callback.StartsWith("change period $"))
                    {
                        callback = callback.Substring("change period $".Length);
                        foreach (var ad in adverts)
                        {
                            if (ad.Name == callback)
                            {
                                await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
                                try
                                {
                                    currentAdvert = ad;
                                    step = AdminStep.Period;
                                    createMessages.Add(Bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat, "Пришлите мне новый период рассылки в формате Д:Ч:М", replyMarkup: new InlineKeyboardMarkup(cancelAdminButton)).Result);
                                }
                                catch
                                { }
                                break;
                            }
                        }
                        return;
                    }
                    if (callback.StartsWith("$"))
                    {
                        callback = callback.Substring(1);
                        foreach (var ad in adverts)
                        {
                            if (ad.Name == callback)
                            {
                                await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
                                string text;
                                if (ad.IsActive)
                                {
                                    text = "Остановить";
                                }
                                else
                                {
                                    if (ad.Count >= ad.MaxCount || ad.MaxCount < 0)
                                    {
                                        text = "Запустить";
                                    }
                                    else
                                    {
                                        text = "Возобновить";
                                    }
                                }
                                InlineKeyboardButton[][] buttons = new[]
                                {
                                        new[] { InlineKeyboardButton.WithCallbackData("Сообщение", "message "+"$"+ad.Name), InlineKeyboardButton.WithCallbackData("Период рассылки", "period " + "$" + ad.Name) },
                                        new[] { InlineKeyboardButton.WithCallbackData("Количество рассылок", "count " + "$" + ad.Name), InlineKeyboardButton.WithCallbackData(text, "change state "+"$"+ad.Name)},
                                        new[]{ InlineKeyboardButton.WithCallbackData("Удалить", "delete " + "$" + ad.Name),InlineKeyboardButton.WithCallbackData("Назад", "all adverts") }
                                };
                                currentAdvert = ad;
                                await Bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, "Рекламная запись " + ad.Name, replyMarkup: new InlineKeyboardMarkup(buttons));
                                break;
                            }
                        }
                    }
                    else
                    {
                        switch (callback)
                        {
                            case "get users":
                                try
                                {
                                    InlineKeyboardButton[][] keyboard= new InlineKeyboardButton[][]
                                    {
                                        new [] {InlineKeyboardButton.WithCallbackData("Количество юзеров","get users count"),InlineKeyboardButton.WithCallbackData("Юзернеймы юзеров","get usernames") },
                                        new []{InlineKeyboardButton.WithCallbackData("Назад", "to main menu") }
                                    };

                                    await Bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, "Что вы хотите получить", replyMarkup: new InlineKeyboardMarkup(keyboard));
                                }
                                catch
                                { 
                                }
                                break;
                            case "get users count":
                                try
                                {
                                    InlineKeyboardButton[][] keyboard = new InlineKeyboardButton[][]
                                    {
                                        new []{InlineKeyboardButton.WithCallbackData("Назад", "get users") }
                                    };
                                    string text = "Количество пользователей бота: "+usersIds.Count;
                                    await Bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, text, replyMarkup: new InlineKeyboardMarkup(keyboard));
                                }
                                catch { }
                                break;
                            case "get usernames":
                                try
                                {
                                    using (ExcelPackage package = new ExcelPackage())
                                    {
                                        List<int> withUsernames = new List<int>();
                                        List<int> withoutUsernames = new List<int>();
                                        foreach (var user in usersIds.Keys)
                                        {
                                            if (usersIds[user].Length > 0)
                                            {
                                                withUsernames.Add(user);
                                            }
                                            else
                                            {
                                                withoutUsernames.Add(user);
                                            }
                                        }
                                        ExcelWorksheet workbook = package.Workbook.Worksheets.Add("Лист 1");
                                        
                                            workbook.SetValue(1, 1, "Id");
                                            workbook.SetValue(1, 2, "Username");
                                            int counter = 2;
                                            string username = "";
                                            foreach (var user in withUsernames)
                                            {
                                                username = usersIds[user];
                                                workbook.SetValue(counter, 1, user.ToString());
                                                workbook.SetValue(counter, 2, username);
                                                counter++;
                                            }
                                            foreach (var user in withoutUsernames)
                                            {
                                                username = usersIds[user];
                                                workbook.SetValue(counter, 1, user.ToString());
                                                workbook.SetValue(counter, 2, username);
                                                counter++;
                                            }
                                        package.SaveAs(File.Create("users.xlsx"));
                                        package.Stream.Position = 0;
                                        await Bot.SendDocumentAsync(e.CallbackQuery.Message.Chat, new InputOnlineFile(package.Stream, "Users (" + DateTime.Now.ToString("yy.MM.dd HH.mm.ss") + ").xlsx"));
                                    }
                                }
                                catch
                                { }
                                break;
                            case "to main menu":
                                try
                                {
                                    List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>()
                                    {
                                        new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Добавить рекламную запись","add advert") }
                                    };
                                    if (adverts.Count > 0)
                                        buttons.Insert(0, new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Посмотреть рекламные записи", "all adverts") });
                                    buttons.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Посмотреть юзеров", "get users") });
                                    buttons.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Выйти", "exit") });
                                    InlineKeyboardMarkup keyboardMarkups = new InlineKeyboardMarkup(buttons);
                                    await Bot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId, "Что вы хотите сделать?", replyMarkup: keyboardMarkups);
                                }
                                catch { }
                                break;
                            case "all adverts":
                                try
                                {
                                    List<List<InlineKeyboardButton>> keyboard = new List<List<InlineKeyboardButton>>();
                                    if (adverts == null || adverts.Count == 0)
                                    {
                                        keyboard.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Назад", "to main menu") });
                                        await Bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, "У вас нет рекламных записей", replyMarkup: new InlineKeyboardMarkup(keyboard));
                                        break;
                                    }

                                    foreach (var ad in adverts)
                                    {
                                        if (keyboard.Count <= 0 || keyboard[^1].Count >= 2)
                                            keyboard.Add(new List<InlineKeyboardButton>());
                                        keyboard[^1].Add(InlineKeyboardButton.WithCallbackData(ad.Name, "$" + ad.Name));
                                    }

                                    if (keyboard.Count <= 0 || keyboard[^1].Count >= 2)
                                        keyboard.Add(new List<InlineKeyboardButton>());

                                    keyboard[^1].Add(InlineKeyboardButton.WithCallbackData("Назад", "to main menu"));
                                    await Bot.EditMessageTextAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId, "Ваши рекламные записи", replyMarkup: new InlineKeyboardMarkup(keyboard));
                                }
                                catch { }
                                break;
                            case "add advert":
                                try
                                {
                                    currentAdvert = new Advert();
                                    step = AdminStep.Name;
                                    foreach (var message in createMessages)
                                    {
                                        Bot.DeleteMessageAsync(message.Chat, message.MessageId);
                                    }
                                    createMessages.Clear();
                                    var msg = await Bot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Какое будет название у рекламной записи?", replyMarkup: new InlineKeyboardMarkup(cancelAdminButton));
                                    createMessages.Add(msg);
                                }
                                catch { }
                                break;
                            case "admin cancel":
                                try
                                {
                                    step = null;
                                    foreach (var msg in createMessages)
                                    {
                                        Bot.DeleteMessageAsync(msg.Chat, msg.MessageId);
                                    }
                                    createMessages.Clear();
                                }
                                catch { }
                                break;
                            case "exit":
                                try
                                {
                                    isAdmined = false;
                                    step = null;
                                    foreach (var msg in createMessages)
                                    {
                                        Bot.DeleteMessageAsync(msg.Chat, msg.MessageId);
                                    }
                                    createMessages.Clear();
                                    Bot.DeleteMessageAsync(e.CallbackQuery.Message.Chat, e.CallbackQuery.Message.MessageId);
                                    await Bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat, "Вы покинули ПРО режим", replyMarkup: replyKeyboard);
                                }
                                catch
                                { }
                                break;
                            default:
                                return;
                        }
                        await Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id);
                    }
                }
                catch { }
            }
            else
            {
                if(e.CallbackQuery.Data=="cancel")
                {
                    try
                    {
                        var userInfo = users[e.CallbackQuery.From.Id];
                        userInfo.Choose = null;
                        userInfo.DialogStep = UserStep.Color;
                        foreach (var msg in userInfo.StepMessages)
                        {
                            Bot.DeleteMessageAsync(userInfo.UserId, msg);
                        }
                        userInfo.StepMessages.Clear();
                    }
                    catch { }
                }
            }
        }

        private async static void DeleteAdminButtons()
        {
            try
            {
                foreach(var msg in createMessages)
                {
                    try
                    {
                        Bot.EditMessageReplyMarkupAsync(msg.Chat, msg.MessageId);
                    }
                    catch
                    {

                    }
                }
            }
            catch
            { }
            try
            {
                createMessages.Clear();
            }
            catch { }
        }

        private static string CreateTimeString(TimeSpan value)
        {
            string res = "";
            string days = value.Days.ToString();
            string hours = value.Hours.ToString();
            string minutes = value.Minutes.ToString();
            bool b = false;
            if (days != "0")
            {
                if (days.Length == 1)
                {
                    b = true;
                    days = "0" + days;
                }
                if (days[^2] == '1' || days[^1] > '4')
                {
                    if (b)
                    {
                        days = days.Substring(1);
                    }
                    res += days + " дней";
                }
                else
                {
                    if (days[^1] == '1')
                    {
                        if (b)
                        {
                            days = days.Substring(1);
                        }
                        res += days + " день";
                    }
                    else
                    {
                        if (b)
                        {
                            days = days.Substring(1);
                        }
                        res += days + " дня";
                    }
                }
            }
            if (hours != "0")
            {
                b = false;
                if (hours.Length == 1)
                {
                    b = true;
                    hours = "0" + hours;
                }
                if (hours[^2] == '1' || hours[^1] > '4')
                {
                    if (res != "")
                    {
                        if (minutes == "0")
                        {
                            res += " и";
                        }
                        else
                        {
                            res += ",";
                        }
                        res += " ";
                    }
                    if (b)
                    {
                        hours = hours.Substring(1);
                    }
                    res += hours + " часов";
                }
                else
                {
                    if (res != "")
                    {
                        if (minutes == "0")
                        {
                            res += " и";
                        }
                        else
                        {
                            res += ",";
                        }
                        res += " ";
                    }
                    if (hours[^1] == '1')
                    {
                        if (b)
                        {
                            hours = hours.Substring(1);
                        }
                        res += hours + " час";
                    }
                    else
                    {
                        if (b)
                        {
                            hours = hours.Substring(1);
                        }
                        res += hours + " часа";
                    }
                }
            }
            if (minutes != "0")
            {
                b = false;
                if (minutes.Length == 1)
                {
                    b = true;
                    minutes = "0" + minutes;
                }
                if (minutes[^2] == '1' || minutes[^1] > '4')
                {
                    if (res != "")
                    {
                        res += " и ";
                    }
                    if (b)
                    {
                        minutes = minutes.Substring(1);
                    }
                    res += minutes + " минут";
                }
                else
                {
                    if (res != "")
                    {
                        res += " и ";
                    }
                    if (minutes[^1] == '1')
                    {
                        if (b)
                        {
                            minutes = minutes.Substring(1);
                        }
                        res += minutes + " минута";
                    }
                    else
                    {
                        if (b)
                        {
                            minutes = minutes.Substring(1);
                        }
                        res += minutes + " минуты";
                    }
                }
            }
            return res;
        }

        private static bool IsAdmin(int id)
        {
            return id == 1132338630;
        }

        private static bool TryParse(string text, out TimeSpan time)
        {
            try
            {
                List<string> data = new List<string>(text.Split(':'));
                if (data.Count == 0)
                {
                    time = new TimeSpan(-1);
                    return false;
                }
                while (data.Count < 3)
                {
                    data.Insert(0, "0");
                }
                time = TimeSpan.FromDays(Math.Abs(int.Parse(data[0])));
                time = time.Add(TimeSpan.FromHours(Math.Abs(int.Parse(data[1]))));
                time = time.Add(TimeSpan.FromMinutes(Math.Abs(int.Parse(data[2]))));
                return true;
            }
            catch
            {
                time = new TimeSpan(-1);
            }
            return false;
        }

        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (processingUsers.Contains(e.Message.From.Id))
            {
                await Bot.SendTextMessageAsync(e.Message.From.Id, "Подождите, я ещё не обработал вашу фотографию");
                return;
            }
            try
            {
                lock (lockObj)
                {
                    try
                    {
                        if (!users.ContainsKey(e.Message.From.Id))
                        {
                            users[e.Message.From.Id] = new UserDialogInfo(e.Message.From.Id) { DialogStep = UserStep.Color };
                        }
                    }
                    catch { }
                    if (!usersIds.ContainsKey(e.Message.From.Id))
                    {
                        usersIds.Add(e.Message.From.Id, e.Message.From.Username ?? "");
                        using (var stream=new FileStream("users", FileMode.OpenOrCreate))
                        {
                            new BinaryFormatter().Serialize(stream, usersIds);
                        }
                    }
                }
            }
            catch { }
            UserDialogInfo dialogInfo = users[e.Message.From.Id];
            try
            {
                if ((isAdmined || e.Message.Text == "admin") && IsAdmin(e.Message.From.Id))
                {
                    if (step == null)
                    {
                        List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>()
                        {
                            new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Добавить рекламную запись","add advert") }
                        };
                        if (adverts.Count > 0)
                            buttons.Insert(0, new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Посмотреть рекламные записи", "all adverts") });
                        buttons.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Посмотреть юзеров", "get users") });
                        buttons.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Выйти", "exit") });
                        InlineKeyboardMarkup keyboardMarkups = new InlineKeyboardMarkup(buttons);
                        if (!isAdmined)
                            await Bot.SendTextMessageAsync(e.Message.Chat.Id, "Добро пожаловать в ПРО режим", replyMarkup: new ReplyKeyboardRemove());
                        await Bot.SendTextMessageAsync(e.Message.Chat.Id, "Что вы хотите сделать?", replyMarkup: keyboardMarkups);
                    }
                    else
                    {

                        createMessages.Add(e.Message);
                        switch (step.Value)
                        {
                            case AdminStep.Name:
                                if (e.Message.Text == null || e.Message.Text == "")
                                {
                                    var msg = await Bot.SendTextMessageAsync(e.Message.From.Id, "Ошибка!\nНазвание некорректно.\nПопробуйте ещё раз");
                                    createMessages.Add(msg);
                                }
                                else
                                {
                                    foreach (var ad in adverts)
                                    {
                                        if (ad.Name == e.Message.Text)
                                        {
                                            var msg = await Bot.SendTextMessageAsync(e.Message.From.Id, "Ошибка!\nРекламная запись с таким названием уже существует.\nВведите другое название");
                                            createMessages.Add(msg);
                                            e.Message.Text = null;
                                        }
                                    }
                                    if (e.Message.Text == null)
                                        break;
                                    currentAdvert.Name = e.Message.Text;
                                    if (currentAdvert.IsCreating)
                                    {
                                        step = AdminStep.Message;
                                        var msg = await Bot.SendTextMessageAsync(e.Message.From.Id, "Отлично!\nТеперь пришлите мне сообщение для рассылки (текст и картинка)", replyMarkup: new InlineKeyboardMarkup(cancelAdminButton));
                                        createMessages.Add(msg);
                                    }
                                }
                                break;
                            case AdminStep.Message:
                                if ((e.Message.Text == null || e.Message.Text == "") && (e.Message.Photo == null || e.Message.Photo.Length <= 0))
                                {
                                    var msg = await Bot.SendTextMessageAsync(e.Message.From.Id, "Ошибка!\nСообщение не содержит ни текста, ни картинки.\nПопробуйте ещё раз");
                                    createMessages.Add(msg);
                                }
                                else
                                {
                                    currentAdvert.Message = new AdvertMessage(e.Message);
                                    if (currentAdvert.IsCreating)
                                    {
                                        step = AdminStep.Period;
                                        var msg = await Bot.SendTextMessageAsync(e.Message.From.Id, "Отлично, осталось немного!\nПришлите мне период рассылки в формате Д:Ч:М", replyMarkup: new InlineKeyboardMarkup(cancelAdminButton));
                                        createMessages.Add(msg);
                                    }
                                    else
                                    {
                                        step = null;
                                        await Bot.SendTextMessageAsync(e.Message.From.Id, "Отлично!\nСообщение для рассылки успешно обновлено");
                                        currentAdvert = null;
                                        SerializeAdverts();
                                        DeleteAdminButtons();
                                    }
                                }
                                break;
                            case AdminStep.Period:
                                if (e.Message.Text == null || !TryParse(e.Message.Text.Replace(" ", ""), out TimeSpan span))
                                {
                                    var msg = await Bot.SendTextMessageAsync(e.Message.From.Id, "Ошибка!\nСообщение не соответсвует формату Д:М:Ч.\nПопробуйте ещё раз");
                                    createMessages.Add(msg);
                                }
                                else
                                {
                                    if (span.TotalMinutes < 5)
                                    {
                                        var msg = await Bot.SendTextMessageAsync(e.Message.From.Id, "Ошибка!\nСлишком короткий промежуток времени (min 5 минут).\nПопробуйте ещё раз");
                                        createMessages.Add(msg);
                                        break;
                                    }
                                    if (currentAdvert.IsCreating)
                                    {
                                        step = AdminStep.Count;
                                        currentAdvert.Period = span;
                                        var msg = await Bot.SendTextMessageAsync(e.Message.From.Id, "И последний штрих.\nПришлите мне количество рассылок этой записи", replyMarkup: new InlineKeyboardMarkup(cancelButton));
                                        createMessages.Add(msg);
                                    }
                                    else
                                    {
                                        step = null;
                                        currentAdvert.Period = span;
                                        await Bot.SendTextMessageAsync(e.Message.From.Id, "Отлично!\nВремя рассылки успешно изменено");
                                        timers[currentAdvert].Interval = currentAdvert.Period.Value.TotalMilliseconds;
                                        currentAdvert = null;
                                        SerializeAdverts();
                                        DeleteAdminButtons();
                                    }
                                }
                                break;
                            case AdminStep.Count:
                                if (e.Message.Text == null || !int.TryParse(e.Message.Text.Replace(" ", ""), out int count))
                                {
                                    var msg = await Bot.SendTextMessageAsync(e.Message.From.Id, "Ошибка!\nСообщение не является целочисленным значением.\nПоопробуйте ещё раз");
                                    createMessages.Add(msg);
                                }
                                else
                                {
                                    if (count < -1)
                                    {
                                        var msg = await Bot.SendTextMessageAsync(e.Message.From.Id, "Ошибка!\nЯ не обнаружил числа в вашем сообщении.\nПопробуйте ещё раз");
                                        createMessages.Add(msg);
                                        break;
                                    }
                                    step = null;
                                    if (currentAdvert.IsCreating)
                                    {
                                        currentAdvert.MaxCount = count;
                                        currentAdvert.Count = 0;
                                        adverts.Add(currentAdvert);
                                        await Bot.SendTextMessageAsync(e.Message.From.Id, "Отлично!\nВы создали рекламную запись " + currentAdvert.Name);
                                        createMessages.Clear();
                                        ThreadTimer timer = new ThreadTimer(currentAdvert.Period.Value.TotalMilliseconds, DateTimeOffset.Now.Add(currentAdvert.Period.Value)) { Tag = currentAdvert };
                                        timer.Elapsed += Program_Elapsed;
                                        timer.Start();
                                        timers[currentAdvert] = timer;
                                        currentAdvert.IsActive = timer.IsRunning;
                                    }
                                    else
                                    {
                                        currentAdvert.MaxCount = count;
                                        currentAdvert.Count = 0;
                                        var timer = timers[currentAdvert];
                                        if (!timer.IsRunning)
                                            timer.Start();
                                        await Bot.SendTextMessageAsync(e.Message.From.Id, "Отлично!\nКоличество рассылок успешно обновлено");
                                        DeleteAdminButtons();
                                    }
                                    currentAdvert = null;
                                    SerializeAdverts();
                                }
                                break;
                        }
                    }
                    isAdmined = true;
                }
                else
                {
                    if (e.Message.Text == "/about")
                    {
                        await Bot.SendTextMessageAsync(e.Message.Chat.Id, about);
                        return;
                    }

                    if (e.Message.Text == "/start")
                    {
                        if (e.Message.From.Username != null)
                            await Bot.SendTextMessageAsync(e.Message.Chat.Id, welcome.Replace("@имя человека", e.Message.From.Username.ToString()), replyMarkup: replyKeyboard);
                        else
                            await Bot.SendTextMessageAsync(e.Message.Chat.Id, welcome.Replace("@имя человека", e.Message.From.FirstName.ToString()), replyMarkup: replyKeyboard);
                        if (!users.ContainsKey(e.Message.From.Id))
                            users[e.Message.From.Id] = new UserDialogInfo(e.Message.From.Id) { DialogStep = UserStep.Color };
                        return;
                    }

                    if (e.Message.Text == "палитра цветов")
                    {
                        await Bot.SendPhotoAsync(e.Message.Chat.Id, new InputOnlineFile(GetPalette()));
                        return;
                    }

                    if (dialogInfo.DialogStep.GetValueOrDefault(UserStep.Image) == UserStep.Color)
                    {
                        if (e.Message.Text != null)
                        {
                            if (colors.ContainsKey(e.Message.Text))
                            {
                                var msgId = Bot.SendTextMessageAsync(e.Message.Chat.Id, afterColorChoice, replyMarkup: new InlineKeyboardMarkup(cancelButton)).Result.MessageId;
                                dialogInfo.StepMessages.Add(e.Message.MessageId);
                                dialogInfo.StepMessages.Add(msgId);
                                dialogInfo.AfterColorMessage = msgId;
                                dialogInfo.Choose = e.Message.Text;
                                dialogInfo.DialogStep = UserStep.Image;
                                return;
                            }
                        }
                    }
                    if (dialogInfo.DialogStep.GetValueOrDefault(UserStep.Image) == UserStep.Image)
                    {
                        if (dialogInfo.DialogStep.GetValueOrDefault(UserStep.Color) != UserStep.Image)
                        {
                            if (dialogInfo.DialogStep == null)
                            {
                                await Bot.SendTextMessageAsync(e.Message.From.Id, "Подождите, я ещё не обработал вашу фотографию");
                            }
                            return;
                        }
                        TelegramPhoto photo = GetPhoto(e.Message);
                        if (photo == null || photo.FileId == null)
                        {
                            dialogInfo.StepMessages.Add(e.Message.MessageId);
                            dialogInfo.StepMessages.Add(Bot.SendTextMessageAsync(e.Message.From.Id, "Я не обнаружил фотографии в вашем сообщении.\nПришлите мне фото для обработки").Result.MessageId);
                        }
                        else
                        {
                            Bot.EditMessageReplyMarkupAsync(dialogInfo.UserId, dialogInfo.AfterColorMessage);
                            dialogInfo.StepMessages.Clear();
                            dialogInfo.DialogStep = null;
                            Thread thread = new Thread(async () =>
                                {
                                    string kol = colors[dialogInfo.Choose];
                                    Message message = await Bot.SendTextMessageAsync(e.Message.From.Id, "Минуточку, обрабатываем вашу фотографию!");
                                    try
                                    {
                                        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "photo_library" + e.Message.From.Id));
                                        using (var file = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "photo_library" + e.Message.From.Id, "photo." + photo.Extension), FileMode.OpenOrCreate))
                                        {
                                            await Bot.GetInfoAndDownloadFileAsync(photo.FileId, file);
                                        }
                                        var chromeOptions = new ChromeOptions();
                                        var chromeDriverService = ChromeDriverService.CreateDefaultService();
                                        chromeDriverService.HideCommandPromptWindow = true;
                                        chromeOptions.AddUserProfilePreference("download.default_directory", Path.Combine(Directory.GetCurrentDirectory(), "photo_library" + e.Message.From.Id));
                                        ChromeDriver driver = new ChromeDriver(chromeDriverService, chromeOptions);
                                        driver.Manage().Window.Size = new Size(795, 920);
                                        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(30);
                                        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
                                        Stopwatch stopwatch = new Stopwatch();
                                        stopwatch.Start();
                                        while (true)
                                        {
                                            try
                                            {
                                                if (stopwatch.Elapsed.TotalSeconds > 60)
                                                {
                                                    Bot.DeleteMessageAsync(message.Chat, message.MessageId);
                                                    await Bot.SendTextMessageAsync(e.Message.Chat, "Упс, что-то пошло не так😕 Попробуйте другую фотку.");
                                                    return;
                                                }
                                                driver.Navigate().GoToUrl("https://www.matrix.com/virtual-hair-color-try-on");
                                                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                                                wait.Until(d => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState;").Equals("complete"));
                                                Thread.Sleep(500);
                                                driver.FindElementByCssSelector("div[datapname='" + kol + "']");
                                                driver.ExecuteScript("scroll(0,80)");
                                                break;
                                            }
                                            catch
                                            {

                                            }
                                        }
                                        stopwatch.Stop();
                                        var f = driver.FindElements(By.Id("upload-field"));
                                        foreach (var g in f)
                                        {
                                            if (g.TagName == "input")
                                            {
                                                g.SendKeys(Path.Combine(Directory.GetCurrentDirectory(), "photo_library" + e.Message.From.Id, "photo." + photo.Extension));
                                            }
                                        }
                                        Thread.Sleep(30000);
                                        try
                                        {
                                            var elem5 = driver.FindElementByClassName("toastersubscribe-section");
                                            var elem4 = elem5.FindElement(By.ClassName("toaster-header"));
                                            elem4.Click();
                                        }
                                        catch
                                        { }
                                        Thread.Sleep(1000);
                                        var elem = driver.FindElementByCssSelector("div[datapname='" + kol + "']");
                                        elem.Click();
                                        Thread.Sleep(1500);
                                        if (driver.FindElementByCssSelector("div[class='error-msg filetype ng-binding']").Text != "")
                                        {
                                            await Bot.SendTextMessageAsync(e.Message.Chat.Id, "Ошибка! На данном фото не распознано лицо!");
                                            driver.Quit();
                                            return;
                                        }
                                        Actions action3 = new Actions(driver);
                                        //action3.MoveByOffset(404, 730).Click().Build().Perform();
                                        action3.MoveByOffset(404, 610).Click().Build().Perform();
                                        Thread.Sleep(9000);
                                        Actions action2 = new Actions(driver);
                                        action2.MoveByOffset(-46, 5).Click().Build().Perform();
                                        Actions action4 = new Actions(driver);
                                        Thread.Sleep(4000);
                                        action4.MoveByOffset(-75, 0).Click().Build().Perform();
                                        Thread.Sleep(500);
                                        int length;
                                        stopwatch.Restart();
                                        while (true)
                                        {
                                            length = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "photo_library" + e.Message.From.Id)).Length;
                                            if (length == 2 || stopwatch.Elapsed.TotalSeconds >= 10)
                                                break;
                                        }
                                        driver.Quit();
                                        string url = "";
                                        foreach (var fil in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "photo_library" + e.Message.From.Id)))
                                        {
                                            if (fil != Path.Combine(Directory.GetCurrentDirectory(), "photo_library" + e.Message.From.Id, "photo." + photo.Extension))
                                                url = fil;
                                        }
                                        File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "photo_library" + e.Message.From.Id, "photo." + photo.Extension));

                                        Bitmap img;
                                        using (var fs = File.OpenRead(url))
                                            img = new Bitmap(fs);
                                        int x2 = img.Width;
                                        int y2 = img.Height - 63;
                                        int width = x2;
                                        int height = y2;
                                        var result = new Bitmap(width, height);
                                        for (int i = 0; i < x2; i++)
                                            for (int j = 0; j < y2; j++)
                                                result.SetPixel(i, j, img.GetPixel(i, j));
                                        using (img)
                                        {
                                            result.Save(url, ImageFormat.Bmp);
                                        }
                                        using (var fs = File.OpenRead(url))
                                        {
                                            Bot.DeleteMessageAsync(message.Chat, message.MessageId);
                                            await Bot.SendPhotoAsync(e.Message.Chat.Id, new InputOnlineFile(fs));
                                            await Bot.SendTextMessageAsync(e.Message.Chat.Id, afterResult);
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        Bot.DeleteMessageAsync(message.Chat, message.MessageId);
                                        await Bot.SendTextMessageAsync(e.Message.Chat, "Упс, что-то пошло не так😕 Попробуйте другую фотку.");
                                        Console.WriteLine("id: " + e.Message.From.Id + "\nError: " + ex.Message);
                                    }
                                    finally
                                    {
                                        try
                                        {
                                            DeleteDirectory(Path.Combine(Directory.GetCurrentDirectory(), "photo_library" + e.Message.From.Id));
                                        }
                                        catch
                                        { }
                                        dialogInfo.DialogStep = UserStep.Color;
                                    }
                                });
                            thread.Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static TelegramPhoto GetPhoto(Message message)
        {
            string fileId = null;
            string extension = "jpg";
            if (message.Photo != null && message.Photo.Length > 0)
            {
                int maxSize = -1;
                foreach (var photo in message.Photo)
                {
                    if (photo.FileSize > maxSize)
                    {
                        maxSize = photo.FileSize;
                        fileId = photo.FileId;
                    }
                }
                return new TelegramPhoto(fileId, extension);
            }
            else
            {
                if (message.Document != null)
                {
                    if (message.Document.MimeType != null)
                    {
                        string type = message.Document.MimeType;
                        if (!type.StartsWith("image"))
                        {
                            return null;
                        }
                        if (type.StartsWith("image/"))
                        {
                            type = type.Substring("image/".Length);
                            if (type == "png" || type == "jpeg" || type == "jpg")
                            {
                                extension = type;
                            }
                        }
                        return new TelegramPhoto(message.Document.FileId, extension);
                    }
                }
            }
            return null;
        }

        private static void DeleteDirectory(string path)
        {
            try
            {
                foreach (var file in Directory.EnumerateFiles(path))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
                Directory.Delete(path);
            }
            catch { }
        }

        private static void SerializeAdverts()
        {
            if (!File.Exists("adverts"))
            {
                using (File.Create("adverts"))
                { }
            }
            using (var file = File.OpenWrite("adverts"))
            {
                new BinaryFormatter().Serialize(file, adverts);
            }
        }

        private static void Program_Elapsed(object sender, EventArgs e)
        {
            var message = (Advert)((ThreadTimer)sender).Tag;
            if (message.Count >= message.MaxCount)
            {
                message.IsActive = false;
                ((ThreadTimer)sender).Stop();
                return;
            }
            try
            {
                if (message.IsActive)
                {
                    foreach (var user in usersIds.Keys)
                    {
                        if (!IsAdmin(user))
                        {
                            try
                            {
                                if (message.Message.FileId == null)
                                {
                                    Bot.SendTextMessageAsync(new ChatId(user), message.Message.Text);
                                }
                                else
                                {
                                    Bot.SendPhotoAsync(new ChatId(user), new InputOnlineFile(message.Message.FileId), caption: message.Message.Text);
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch
            {

            }
            try
            {
                if (message.MaxCount > 0)
                {
                    message.Count++;
                    if (message.Count >= message.MaxCount)
                    {
                        message.IsActive = false;
                        ((ThreadTimer)sender).Stop();
                    }
                }
            }
            catch
            { }
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            SerializeAdverts();
            lock (lockObj)
            {
                using (StreamWriter writer = new StreamWriter(new FileStream("users", FileMode.OpenOrCreate)))
                {
                    foreach (var user in users)
                    {
                        writer.Write(user + " ");
                    }
                }
            }
            try
            {
                Bot.StopReceiving();
            }
            catch { }
        }

        private static string GetPalette()
        {
            return "AgACAgIAAxkDAAIH1l8xkwZFvZesFamk4CVB_47y9Az8AAJGsDEbng1BSeGvzJyzBDJsOGxAli4AAwEAAwIAA3gAA-EwAAIaBA";
        }
    }
}
