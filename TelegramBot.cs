using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Threading;
using CheckSaturday.ScheduleProcessors;
using Telegram.Bot.Types.Enums;

namespace CheckSaturday;

/// <summary>
/// Класс, отвечающий за взаимодействие с пользователем
/// </summary>
public static class TelegramBot
{
    private static ITelegramBotClient _tgClient;

    public static async Task Start(string token)
    {
        while (true)
        {
            try
            {
                await Task.Delay(1000);
                _tgClient ??= new TelegramBotClient(token);

                var updates = await _tgClient.GetUpdatesAsync();
                if (updates.Length == 0) continue;
                // Сбрасываем счётчик неполученных апдейтов
                await _tgClient.GetUpdatesAsync(offset: updates.Max(x => x.Id) + 1);

                var messages = updates.Where(x => x.IsMessageType());
                // Документы, отправленные группой, будут обработаны вместе, иначе -- поотдельности
                var documentGroups = updates.Where(x => x.IsDocumentType())
                    .GroupBy(x => x.Message?.MediaGroupId ?? x.Id.ToString());

                foreach (var message in messages)
                {
                    try
                    {
                        await HandleMessage(message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error >> Ошибка обработки сообщения: {e.Message}\n{e.StackTrace}");
                    }
                }

                foreach (var document in documentGroups)
                {
                    try
                    {
                        await HandleDocuments(document);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error >> Ошибка обработки сообщения: {e.Message}\n{e.StackTrace}");
                    }
                }

                LogRequests(updates);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error [{DateTime.Now:G}] >> Ошибка TGBot.Start(): {e.Message}\n{e.StackTrace}");
                _tgClient = null;
            }
        }
    }

    // TODO: Съедобен ли формат документа
    private static async Task HandleDocuments(IEnumerable<Update> updates)
    {
        string message;
        await _tgClient.SendTextMessageAsync(updates.First().GetChatId(), "Словил доки.");
        try
        {
            var couples = await ScheduleFromTelegram.GetCouplesFromUpdates(updates, _tgClient);
            message = CouplesReport.BuildMessage(couples, needBeActual: false);
        }
        catch
        {
            message = "Документ несъедобен или реально пар нет...";
        }

        await _tgClient.SendTextMessageAsync(updates.First().GetChatId(), message);
    }

    private static async Task HandleMessage(Update update)
    {
        var msg = update.Message.Text.ToLower().Trim();

        switch (msg)
        {
            case "/start":
                await _tgClient.SendTextMessageAsync(update.GetChatId(),
                    "Возрадуйтесь! Теперь можно автоматизировано проверять, есть ли пары во втором корпусе!\n" +
                    "Напишите <i>/check</i> для проверки наличия пар в субботу на кафедре ИОТС", ParseMode.Html);
                break;
            case "/check":
                await CheckCouples(update);
                break;
        }
    }


    private static async Task CheckCouples(Update update)
    {
        var message = CouplesReport.BuildMessage(ScheduleStaticCover.Couples, needBeActual: true);
        await _tgClient.SendTextMessageAsync(update.GetChatId(), message);
    }

    private static long GetChatId(this Update update)
    {
        return update.IsCallbackType() ? update.CallbackQuery.Message!.Chat.Id : update.Message.Chat.Id;
    }

    private static bool IsMessageType(this Update update) =>
        update.Type == UpdateType.Message && update.Message?.Text != null;

    private static bool IsCallbackType(this Update update) =>
        update.Type == UpdateType.CallbackQuery;

    private static bool IsDocumentType(this Update update) =>
        update?.Message?.Document is { MimeType: "application/vnd.ms-excel" or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" };

    private static void LogRequests(IEnumerable<Update> updates)
    {
        foreach (var update in updates)
        {
            var from = update.Message?.From;
            var date = DateTime.Now;
            var type = update.IsDocumentType() ? "Документ" : "Запрос";
            Console.WriteLine($"TG_BOT >> {type} от {from?.FirstName} {from?.LastName} [@{from?.Username}] || Дата: {date:g}\n");
        }
    }
}