using System;
using System.IO;
using System.Threading;
using CheckSaturday;
using CheckSaturday.ScheduleProcessors;


if (args.Length != 1) throw new Exception("Не указан токен для бота.");
SetUpdateScheduleTimer();

while (ScheduleStaticCover.Couples is null) Thread.Sleep(100);
_ = TelegramBot.Start(args[0]);

Console.WriteLine("Press 'q' to stop program and exit...");
while (true)
{
    var key = Console.ReadKey();
    if (key.Key == ConsoleKey.Q)
    {
        Directory.Delete(ScheduleFromTelegram.CachePath, true);
        return;
    }
}


static async void SetUpdateScheduleTimer()
{
    await ScheduleDownloader.CheckUpdate();
    ScheduleStaticCover.Update(ScheduleDownloader.CacheDir);

    var UpdateInterval = new TimeSpan(hours: ScheduleDownloader.HoursCacheIsActual, minutes: 5, seconds: 0);
    var UpdateTimer = new System.Timers.Timer(UpdateInterval);
    UpdateTimer.Elapsed += (s, e) => FullUpdate();

    UpdateTimer.AutoReset = true;
    UpdateTimer.Enabled = true;
    UpdateTimer.Start();
}

static async void FullUpdate()
{
    try
    {
        if (await ScheduleDownloader.CheckUpdate())
            ScheduleStaticCover.Update(ScheduleDownloader.CacheDir);
    }
    catch
    {
        Console.WriteLine("Error >> Не удалось обновить расписание.");
    }
}