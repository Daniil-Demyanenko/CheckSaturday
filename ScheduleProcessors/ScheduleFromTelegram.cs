using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CheckSaturday.InstituteParsers;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = System.IO.File;
using TgFile = Telegram.Bot.Types.File;

namespace CheckSaturday.ScheduleProcessors;

public class ScheduleFromTelegram
{
    public static readonly string CachePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Temp");
    
    public static async Task<IEnumerable<ClassInfo>> GetCouplesFromUpdates(IEnumerable<Update> updates, ITelegramBotClient TBClient)
    {
        string dirPath = GetDirName();
        Directory.CreateDirectory(dirPath);

        foreach (var update in updates)
            await DownloadFile(update, TBClient, dirPath);
        
        var schedule = new Schedule();
        schedule.Update(dirPath);
        
        Directory.Delete(dirPath, recursive: true);

        if (!schedule.Couples.Any()) throw new Exception("No couples found.");
        return schedule.Couples;
    }

    private static async Task DownloadFile(Update update, ITelegramBotClient TBClient, string dirPath)
    {
        var file = await TBClient.GetFileAsync(update.Message.Document.FileId);
        var fileName = Path.Combine(dirPath, file.FileId[^8..] + "." + file.FilePath.Split('.').Last());

        await using var saveFileStream = File.Open(fileName, FileMode.Create);
        await TBClient.DownloadFileAsync(file.FilePath, saveFileStream);
    }

    private static string GetDirName()
    {
        if (!Directory.Exists(CachePath)) Directory.CreateDirectory(CachePath);
        return Path.Combine(CachePath, Guid.NewGuid().ToString()[0..8]);
    }
}