using System;
using System.Threading;
using System.Linq;
using System.Text.RegularExpressions;
using CheckSaturday;
using static System.Console;
using CheckSaturday.InstituteParsers;


ScheduleDownloader.CheckUpdate();
CoupleSchedule.Update(ScheduleDownloader.CacheDir);

while (CoupleSchedule.Couples is null) Thread.Sleep(100);

var saturdayCouples = CoupleSchedule.Couples.Where(x => x.Day.ToLower().Contains("субб") && ActualAuditNumber(x));

var maxDate = CoupleSchedule.Couples.Max(x => x.Date);

WriteLine($"Расписание актуально до: {maxDate.ToString("d")}\n");

if (saturdayCouples.Count() == 0)
    WriteLine("Добби свободен");
else
    foreach(var i in saturdayCouples)
        WriteLine($"{i.Time}\t {i.Course}-{i.Group}\t {i.Title}");



bool ActualAuditNumber(ClassInfo c)
{
    var posibleNumbers = new string[] { "152", "151", "153", "156" };

    var adit = Regex.Match(c.Title, @"\b\d{1,}-{0,1}\d{2,}\w{0,1}");
    if (posibleNumbers.Any(x => x == adit.Value.Trim())) return true;

    return false;
}