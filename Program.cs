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
        try
        {
            Directory.Delete(ScheduleFromTelegram.CachePath, true);
        }
        catch
        {
            Console.WriteLine("Main >> дирректория не была создана");
        }

        return;
    }
}


static async void SetUpdateScheduleTimer()
{
    await ScheduleDownloader.CheckUpdate();
    ScheduleStaticCover.Update(ScheduleDownloader.CacheDir);

    var updateInterval = new TimeSpan(hours: ScheduleDownloader.HoursCacheIsActual, minutes: 5, seconds: 0);
    var updateTimer = new System.Timers.Timer(updateInterval);
    updateTimer.Elapsed += (_, _) => FullUpdate();

    updateTimer.AutoReset = true;
    updateTimer.Enabled = true;
    updateTimer.Start();
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