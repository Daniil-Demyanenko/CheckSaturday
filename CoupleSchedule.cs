using System;
using System.Collections.Generic;
using System.IO;
using CheckSaturday.InstituteParsers;

namespace CheckSaturday;

/// <summary>
/// Глобальное расписание пар университета
/// </summary>
public static class CoupleSchedule
{
    /// <summary>
    /// Список пар всех институтов
    /// </summary>
    public static IEnumerable<ClassInfo> Couples => _Couples;
    /// <summary>
    /// Список всех учебных групп
    /// </summary>
    public static IEnumerable<StudyGroup> StudyGroups => _StudyGroups;

    private static List<StudyGroup> _StudyGroups;
    private static List<ClassInfo> _Couples;



    /// <summary>
    /// Заново распарсить и заполнить данные о расписании
    /// </summary>
    public static void Update(string CachePath)
    {
        var files = Directory.GetFiles(CachePath);

        var tempCouples = new List<ClassInfo>();
        var tempStudyGroups = new List<StudyGroup>();

        foreach (var file in files)
        {
            try
            {
                using var IFMOIOT = new TemplateScheduleParser(file);
                tempCouples.AddRange(IFMOIOT.Parse());
                tempStudyGroups.AddRange(IFMOIOT.StudyGroups);
            }
            catch
            {
                Console.WriteLine($"Error >> Не получилось спарсить файл: {file}");
            }
        }

        _Couples = tempCouples;
        _StudyGroups = tempStudyGroups;
        Console.WriteLine($"Info >> Обновлено {_Couples.Count} пар.");
    }

}