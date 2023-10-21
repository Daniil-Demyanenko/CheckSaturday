using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CheckSaturday.InstituteParsers;

namespace CheckSaturday;

public static class CouplesReport
{
    public static string BuildMessage()
    {
        var maxDate = Schedule.Couples.Max(x => x.Date);
        StringBuilder sb = new($"Расписание актуально до {maxDate.ToString("d")}.\n\n");
        if (maxDate.Date < DateTime.Now.Date)
        {
            sb.AppendLine("Кароче, опять не опубликовали вовремя новое расписание.");
            return sb.ToString();
        }

        var sortStartTime = new TimeOnly(16, 00);
        var weekdaysCouples = Schedule.Couples.Where(x =>
            !x.Day.ToLower().Contains("субб") && ActualAuditNumber(x) &&
            GetTimeOfCouple(x.Time) >= sortStartTime).OrderBy(x => x.Date).ThenBy(x => GetTimeOfCouple(x.Time));

        var saturdayCouples = Schedule.Couples.Where(x => x.Day.ToLower().Contains("субб") && ActualAuditNumber(x))
            .OrderBy(x=>x.Date).ThenBy(x => GetTimeOfCouple(x.Time));

        var saturdayInfo = FindCouples(saturdayCouples);
        var weekdaysInfo = FindCouples(weekdaysCouples);

        sb.AppendLine(!weekdaysInfo.Any()
            ? "Хз, чё там с парами после 17:00 в рабочие дни. Может есть, может нет. Я не нашёл у ИФМОИОТа.\n"
            : "Нашёл пары после 17:00 в рабочие дни:\n");
        foreach (var i in weekdaysInfo)
            sb.AppendLine(i);

        sb.Append(!saturdayInfo.Any()
            ? "\nПары ИФМОИОТ'а на субботу отсутствуют.\nИнженерка, спи спокойно."
            : "Найдены пары ИФМОИОТа в субботу:\n");
        foreach (var i in saturdayInfo)
            sb.AppendLine(i);

        return sb.ToString();
    }

    private static List<string> FindCouples(IEnumerable<ClassInfo> couples)
    {
        List<string> coupleInfo = new();

        if (!couples.Any()) return coupleInfo;

        foreach (var i in couples)
            coupleInfo.Add(BuildCoupleInfo(i));

        return coupleInfo;
    }

    private static TimeOnly GetTimeOfCouple(string time)
    {
        string startTime = time.Split('-', StringSplitOptions.RemoveEmptyEntries)[0];
        var separatedTime = startTime.Split(".,:;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

        int.TryParse(separatedTime[0], out int hours);
        int.TryParse(separatedTime[1], out int minutes);

        var res = new TimeOnly(hours, minutes);
        return res;
    }

    private static string BuildCoupleInfo(ClassInfo c)
        => $"- - - - -\n{c.Date:d} || {c.Time} || {c.Course}-{c.Group}\n{c.Title}\n";

    private static bool ActualAuditNumber(ClassInfo c)
    {
        var possibleNumbers = new[] { "151", "152", "153", "154", "155", "156", "157", "159" };

        var audit = Regex.Match(c.Title, @"\b\d{1,}-{0,1}\d{2,}\w{0,1}$");
        if (possibleNumbers.Any(x => x == audit.Value.Trim())) return true;

        return false;
    }
}