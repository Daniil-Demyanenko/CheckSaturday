using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using System.Threading;
using System.Text.RegularExpressions;
using CheckSaturday.InstituteParsers;

namespace CheckSaturday;

/// <summary>
/// Класс, отвечающий за взаимодействие с пользователем
/// </summary>
public static class TelegramBot
{
    private static ITelegramBotClient TBClient;

    public static async void Start(string token)
    {
        bool needToRestart = true;

        while (true) // Небольшой костыль из-за странности работы либы. В документации решения не нашёл.
        {
            if (needToRestart)
            {
                needToRestart = false;

                TBClient = new TelegramBotClient(token);
                TBClient.StartReceiving(async (tbc, u, ct) =>
                {
                    try { await HandleUpdate(tbc, u, ct); }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error >> Ошибка обработки сообщения: {e.Message}");
                        needToRestart = true;
                        return;
                    }
                }, HandleError);
            }
            else await Task.Delay(1000);
        }
    }


    private async static Task HandleUpdate(ITelegramBotClient TBClient, Update update, CancellationToken ct)
    {
        if (!update.IsMessageType() || update?.Message?.Text == null) return; // Да, если тип апдейта -- Message, то не факт, что у него будет поле Message, и если есть поле, не факт, что у него будет поле Text. Очень крутая либа...

        var msg = update.Message.Text.ToLower().Trim();

        switch (msg)
        {
            case "/start":
                await TBClient.SendTextMessageAsync(update.GetChatID(), "Возрадуйтесь! Теперь можно автоматизировано проверять, есть ли пары во втором корпусе!");
                break;
            case "/check":
                await CheckCouples(update);
                break;
        }

        var f = update.Message.From;
        Console.WriteLine($"TG_BOT >> Запрос от {f.FirstName} {f?.LastName} {f?.Username} || Текст: {update.Message.Text}\n");

        return;
    }

    private static async Task CheckCouples(Update update)
    {
        var saturdayCouples = CoupleSchedule.Couples.Where(x => x.Day.ToLower().Contains("субб") && ActualAuditNumber(x));

        var maxDate = CoupleSchedule.Couples.Max(x => x.Date);

        StringBuilder sb = new($"Расписание актуально до: {maxDate.ToString("d")}\n");

        if (saturdayCouples.Count() == 0)
            sb.Append("Пары ИФМОИОТ'а отсутствуют.\nДобби свободен.");
        else
        {
            sb.Append("Найдены пары в субботу\n");
            foreach (var i in saturdayCouples)
                sb.Append($"- - - - -\n{i.Time} || {i.Course}-{i.Group}\n{i.Title}\n\n");
        }

        await TBClient.SendTextMessageAsync(update.GetChatID(), sb.ToString());

    }

    static bool ActualAuditNumber(ClassInfo c)
    {
        var posibleNumbers = new string[] { "152", "151", "153", "156" };

        var adit = Regex.Match(c.Title, @"\b\d{1,}-{0,1}\d{2,}\w{0,1}$");
        if (posibleNumbers.Any(x => x == adit.Value.Trim())) return true;

        return false;
    }

    private static long GetChatID(this Update update)
    {
        if (update.IsCallbackType())
            return update.CallbackQuery.Message.Chat.Id;
        return update.Message.Chat.Id;
    }

    private static bool IsMessageType(this Update update) => update.Type == Telegram.Bot.Types.Enums.UpdateType.Message;
    private static bool IsCallbackType(this Update update) => update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery;

    private static void HandleError(ITelegramBotClient tbc, Exception e, CancellationToken ct)
    {
        Console.WriteLine($"Error >> Ошибка бота: {e.Message}");
    }
}
