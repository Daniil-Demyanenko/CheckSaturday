using System;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using System.Threading;
using Telegram.Bot.Types.Enums;

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
                    try
                    {
                        await HandleUpdate(tbc, u, ct);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error >> Ошибка обработки сообщения: {e.Message}");
                        needToRestart = true;
                    }
                }, HandleError);
            }
            else await Task.Delay(1000);
        }
    }


    private static async Task HandleUpdate(ITelegramBotClient TBClient, Update update, CancellationToken ct)
    {
        if (!update.IsMessageType() || update?.Message?.Text == null)
            return; // Да, если тип апдейта -- Message, то не факт, что у него будет поле Message, 
        // и если есть поле, не факт, что у него будет поле Text. Очень крутая либа...

        var msg = update.Message.Text.ToLower().Trim();

        switch (msg)
        {
            case "/start":
                await TBClient.SendTextMessageAsync(update.GetChatId(),
                    "Возрадуйтесь! Теперь можно автоматизировано проверять, есть ли пары во втором корпусе!\n" +
                    "Напишите <i>/check</i> для проверки наличия пар в субботу на кафедре ИОТС", ParseMode.Html);
                break;
            case "/check":
                await CheckCouples(update);
                break;
        }

        var f = update.Message.From;
        Console.WriteLine(
            $"TG_BOT >> Запрос от {f.FirstName} {f?.LastName} {f?.Username} || Текст: {update.Message.Text}\n");
    }

    private static async Task CheckCouples(Update update)
    {
        var message = CouplesChecker.BuildMessage();
        await TBClient.SendTextMessageAsync(update.GetChatId(), message);
    }
    
    private static long GetChatId(this Update update)
    {
        if (update.IsCallbackType())
            return update.CallbackQuery.Message!.Chat.Id;
        return update.Message.Chat.Id;
    }

    private static bool IsMessageType(this Update update) => update.Type == UpdateType.Message;

    private static bool IsCallbackType(this Update update) =>
        update.Type == UpdateType.CallbackQuery;

    private static void HandleError(ITelegramBotClient tbc, Exception e, CancellationToken ct)
    {
        Console.WriteLine($"Error >> Ошибка бота: {e.Message}");
    }
}