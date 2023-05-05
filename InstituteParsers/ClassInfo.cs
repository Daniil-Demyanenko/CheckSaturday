using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CheckSaturday.InstituteParsers;

/// <summary>
/// Описание пары
/// </summary>
/// <value></value>
public record ClassInfo
{
    /// <summary>
    /// Название предмета
    /// </summary>
    public string Title { get; init; }
    /// <summary>
    /// День недели
    /// </summary>
    public string Day { get; init; }
    /// <summary>
    /// Полная дата
    /// </summary>
    public DateTime Date { get; init; }
    /// <summary>
    /// Название группы
    /// </summary>
    public string Group { get; init; }
    /// <summary>
    /// Номер курса
    /// </summary>
    public int Course { get; init; }
    /// <summary>
    /// Время пары
    /// </summary>
    public string Time { get; init; }



    public ClassInfo(string title, DateTime date, string day, string time, string group, int course) =>
        (Title, Date, Day, Time, Group, Course) = (title, date, day, time, group, course);
}
