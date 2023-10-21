using System.Collections.Generic;
using CheckSaturday.InstituteParsers;
using Telegram.Bot.Types;

namespace CheckSaturday.ScheduleProcessors;

/// <summary>
/// Статичная оболочка для класса расписания (Schedule)
/// </summary>
public class ScheduleStaticCover
{
    /// <summary>
    /// Список пар всех институтов
    /// </summary>
    public static IEnumerable<ClassInfo> Couples => _schedule.Couples;
    
    /// <summary>
    /// Список всех учебных групп
    /// </summary>
    public static IEnumerable<StudyGroup> StudyGroups => _schedule.StudyGroups;
    
    private static Schedule _schedule = new Schedule();

    public static void Update(string cachePath)
    {
        _schedule.Update(cachePath);
    }
}