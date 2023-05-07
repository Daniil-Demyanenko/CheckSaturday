using System;
using System.Threading;
using CheckSaturday;


if (args.Length != 1) throw new Exception("Не указан токен для бота.");
SetUpdateScheduleTimer();

while (CoupleSchedule.Couples is null) Thread.Sleep(100);
TelegramBot.Start(args[0]);

Console.WriteLine("Press 'q' to stop program and exit...");
while (true)
{
    var key = Console.ReadKey();
    if (key.Key == ConsoleKey.Q) return;
}



static void SetUpdateScheduleTimer()
{
    ScheduleDownloader.CheckUpdate();
    CoupleSchedule.Update(ScheduleDownloader.CacheDir);

    var UpdateInterval = new TimeSpan(hours: 4, minutes: 5, seconds: 0);
    var UpdateTimer = new System.Timers.Timer(UpdateInterval);
    UpdateTimer.Elapsed += (s, e) => FullUpdate();

    UpdateTimer.AutoReset = true;
    UpdateTimer.Enabled = true;
    UpdateTimer.Start();
}

static void FullUpdate()
{
    try
    {
        if (ScheduleDownloader.CheckUpdate())
            CoupleSchedule.Update(ScheduleDownloader.CacheDir);
    }
    catch
    {
        Console.WriteLine("Error >> Не удалось обновить расписание.");
    }
}
