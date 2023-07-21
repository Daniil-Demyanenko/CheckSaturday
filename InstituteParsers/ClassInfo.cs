using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CheckSaturday.InstituteParsers;

/// <summary>
/// Описание пары
/// </summary>
/// <value></value>
public record ClassInfo(string Title, DateTime Date, string Day, string Time, string Group, int Course)
{
    /// <summary>
    /// Название предмета
    /// </summary>
    public string Title { get; init; } = Title;

    /// <summary>
    /// День недели
    /// </summary>
    public string Day { get; init; } = Day;

    /// <summary>
    /// Полная дата
    /// </summary>
    public DateTime Date { get; init; } = Date;

    /// <summary>
    /// Название группы
    /// </summary>
    public string Group { get; init; } = Group;

    /// <summary>
    /// Номер курса
    /// </summary>
    public int Course { get; init; } = Course;

    /// <summary>
    /// Время пары
    /// </summary>
    public string Time { get; init; } = Time;
}
