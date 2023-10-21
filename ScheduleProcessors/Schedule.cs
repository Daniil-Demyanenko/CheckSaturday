using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CheckSaturday.InstituteParsers;

namespace CheckSaturday.ScheduleProcessors;

/// <summary>
/// Глобальное расписание пар университета
/// </summary>
public class Schedule
{
    /// <summary>
    /// Список пар всех институтов
    /// </summary>
    public IEnumerable<ClassInfo> Couples => _couples;
    /// <summary>
    /// Список всех учебных групп
    /// </summary>
    public IEnumerable<StudyGroup> StudyGroups => _studyGroups;

    private List<StudyGroup> _studyGroups;
    private List<ClassInfo> _couples;



    /// <summary>
    /// Заново распарсить и заполнить данные о расписании
    /// </summary>
    public void Update(string cachePath)
    {
        var files = Directory.GetFiles(cachePath);

        var tempCouples = new List<ClassInfo>();
        var tempStudyGroups = new List<StudyGroup>();

        files.AsParallel().ForAll((file)=>{
            try
            {
                using var ifmoiot = new TemplateScheduleParser(file);
                tempCouples.AddRange(ifmoiot.Couples);
                tempStudyGroups.AddRange(ifmoiot.StudyGroups);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error >> Не получилось спарсить файл: {file}. Ошибка: {e.Message}");
            }
        });

        _couples = tempCouples;
        _studyGroups = tempStudyGroups;
        Console.WriteLine($"Info >> Обновлено {_couples.Count} пар.");
    }
}