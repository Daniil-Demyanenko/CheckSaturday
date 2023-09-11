using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Aspose.Cells;

#nullable enable

namespace CheckSaturday.InstituteParsers;

// TODO:
// Поиск имён преподавателей, регулярка: \b[\w]{4,}\b\s+\w\.\s*\w\.*
// Поиск номера аудитории, регулярка: \b\d{1,}-{0,1}\d{2,}\w{0,1}

/// <summary>
/// Парсер шаблона ИФМОИОТ
/// </summary>
public class TemplateScheduleParser : IDisposable
{
    public List<StudyGroup> StudyGroups
    {
        get
        {
            if (_studyGroupsResult is null) Parse();
            return _studyGroupsResult!;
        }
    }

    public List<ClassInfo> Couples
    {
        get
        {
            if (_couplesResult is null) Parse();
            return _couplesResult!;
        }
    }

    private List<StudyGroup>? _studyGroupsResult;
    private List<ClassInfo>? _couplesResult;


    // Позиции групп в расписании, не по порядку, исключая удалённые специализации и т.д. 
    private List<int>? _groupNamePositions;
    private int _groupNameRow = 5; // Строка с названиями групп
    private CellPosition _firstVisibleCell;
    private int _maxDataCol;
    private int _maxDataRow;
    private Workbook _workbook;
    private Worksheet _sheet;

    public TemplateScheduleParser(string path)
    {
        _workbook = new Workbook(path);
        _sheet = FindPageWithSchedule();
        _firstVisibleCell = GetFirstVisibleCell();
        _maxDataCol = _sheet.Cells.MaxDataColumn;
        _maxDataRow = _sheet.Cells.MaxDataRow;

        _couplesResult = null;
        _studyGroupsResult = null;
    }

    ~TemplateScheduleParser()
    {
        Dispose();
    }

    public void Dispose()
    {
        _sheet?.Dispose();
        _workbook?.Dispose();
    }

    private void Parse()
    {
        List<ClassInfo> result = new();

        _groupNamePositions = GetGroupNamePositions();
        var dayPos = GetDaysRowInformation();

        foreach (var pos in _groupNamePositions)
            result.AddRange(GetGroupClasses(pos, dayPos));


        _couplesResult = result;
        _studyGroupsResult = GetStudyGroups(_groupNamePositions);
    }

    private List<StudyGroup> GetStudyGroups(List<int> groupsPos)
    {
        List<StudyGroup> result = new();

        foreach (var pos in groupsPos)
        {
            var (course, groupName) = SplitGroupName(colWithGroup: pos);

            result.Add(new StudyGroup(course, groupName));
        }

        return result;
    }

    /// <summary>
    /// Возвращает позиции столбцов с названиями групп
    /// </summary>
    private List<int> GetGroupNamePositions()
    {
        List<int> result = new();

        for (int i = _firstVisibleCell.Col; i < _maxDataCol; i++)
        {
            var cellValue = _sheet.Cells[_groupNameRow + 1, i].Value?.ToString()?.Trim();

            if (cellValue is not null && !_sheet.Cells.Columns[i].IsHidden) //Ячейка имеет значение, не является скрытой
            {
                if (_sheet.Cells[_groupNameRow, i].Value?.ToString()?.Trim() == "группа") break;
                result.Add(i);
            }
        }

        return result;
    }

    /// <summary>
    /// Возвращает информацию о не скрытых днях недели в расписании
    /// </summary>
    private List<DayData> GetDaysRowInformation()
    {
        List<DayData> dayPosition = new();

        for (int i = _firstVisibleCell.Row; i < _maxDataRow; i++)
        {
            var cellValue = _sheet.Cells[i, 0].Value?.ToString()?.Trim();

            if (cellValue is not null && //Ячейка имеет значение, содержит день недели, не является скрытой
                IsContainDayOfWeek(cellValue) &&
                !_sheet.Cells.Rows[i].IsHidden)
            {
                CultureInfo culture = new("ru-RU");
                string dateStr = _sheet.Cells[i, 1].Value?.ToString()?.Trim()!;
                DateTime date = DateTime.Parse(dateStr, culture);
                dayPosition.Add(new DayData(Pos: i, Name: cellValue, Date: date));
            }
        }

        return dayPosition;
    }

    /// <summary>
    /// Возвращает информацию о парах для определённой группы
    /// </summary>
    /// <param name="col">колонка с группой</param>
    /// <param name="dayPos">Позиции дней недели</param>
    private List<ClassInfo> GetGroupClasses(int col, List<DayData> dayPos)
    {
        var result = new List<ClassInfo>();
        int course;
        string groupName;

        (course, groupName) = SplitGroupName(colWithGroup: col);

        foreach (var day in dayPos)
        {
            int classesCountOfDay = _sheet.Cells[day.Pos, 0].GetMergedRange().RowCount;
            for (int i = 0; i < classesCountOfDay; i++)
            {
                int rowWithCouple = day.Pos + i;
                if (_sheet.Cells.Rows[rowWithCouple].IsHidden) continue;
                string? className = _sheet.Cells[rowWithCouple, col].Value?.ToString();
                if (className is null) continue;

                var time = _sheet.Cells[rowWithCouple, 2].Value.ToString()?.Trim();
                var classItem = new ClassInfo(className, day.Date, day.Name, time, groupName, course);
                result.Add(classItem);
            }
        }

        return result;
    }

    /// <summary>
    /// Разделяет название группы на Курс и Направление подготовки.
    /// Добавляет уточнение для объеденённых групп в название
    /// </summary>
    /// <param name="colWithGroup">индекс колонки с названием группы</param>
    /// <returns>(Курс, Направление подготовки)</returns>
    private (int, string) SplitGroupName(int colWithGroup)
    {
        int course;
        string groupName;

        if (_sheet.Cells[_groupNameRow, colWithGroup].IsMerged)
            (course, groupName) = SplitGroupNameForMerged(colWithGroup);
        else (course, groupName) = SplitGroupNameForNotMerged(colWithGroup);

        return (course, groupName);
    }

    /// <summary>
    /// Разделяет название группы на Курс и Направление подготовки
    /// </summary>
    /// <param name="colWithGroup">индекс колонки с названием группы</param>
    /// <returns>(Курс, Направление подготовки)</returns>
    private (int, string) SplitGroupNameForNotMerged(int colWithGroup)
    {
        string[] groupTitle = _sheet.Cells[_groupNameRow, colWithGroup].Value.ToString()!.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries); // Полное название группы
        string groupName = String.Join(' ', groupTitle[1..]).Trim(); // Только название группы
        int course = int.Parse(groupTitle[0]); // Только курс группы

        return (course, groupName);
    }

    /// <summary>
    /// Разделяет название группы на Курс и Направление подготовки.
    /// Добавляет уточнение для объеденённых групп в название
    /// </summary>
    /// <param name="colWithGroup">индекс колонки с названием группы</param>
    /// <returns>(Курс, Направление подготовки)</returns>
    private (int, string) SplitGroupNameForMerged(int colWithGroup)
    {
        bool isSecondCell =
            _sheet.Cells[_groupNameRow, colWithGroup].Value == null; // Является второй ячейкай в объединении?

        if (isSecondCell) colWithGroup--;

        (int course, string groupName) = SplitGroupNameForNotMerged(colWithGroup);

        string appendName =
            _sheet.Cells[_groupNameRow + 1, isSecondCell ? colWithGroup + 1 : colWithGroup].Value.ToString()
                ?.Trim()[0..^1].Trim()!;
        groupName += $" [{appendName}]";


        return (course, groupName);
    }


    // Возвращает ячейку, с которой начинается само расписание (без шапки, скрытых строк/столбцов)
    private CellPosition GetFirstVisibleCell()
    {
        for (var row = 7; row < 1000; row++)
        {
            if (_sheet.Cells.Rows[row].IsHidden) continue;
            for (int col = 3; col < 1000; col++)
            {
                if (_sheet.Cells.Columns[col].IsHidden) continue;

                return new CellPosition(col, row);
            }
        }

        throw new Exception("Bad Exel file");
    }


    // Ищем нужную страницу в файле расписания
    private Worksheet FindPageWithSchedule()
    {
        foreach (var sheet in _workbook.Worksheets)
        {
            if (sheet.Cells.MaxDataColumn >= 10 && sheet.Cells.MaxDataRow >= 7) return sheet;
        }

        throw new Exception($"Не найдено страницы с расписанием в файле {_workbook.AbsolutePath}");
    }

    /// <summary>
    /// Строка является названием дня недели?
    /// </summary>
    private static bool IsContainDayOfWeek(string str)
    {
        var days = new[] { "понедельник", "вторник", "среда", "четверг", "пятница", "суббота", "воскресенье" };
        return days.Any(day => str.Contains(day, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Положение и название дня недели
    /// </summary>
    private record DayData(int Pos, string Name, DateTime Date)
    {
        /// <summary>
        /// Столбец группы
        /// </summary>
        public readonly int Pos = Pos;

        public readonly string Name = Name;
        public readonly DateTime Date = Date;
    }
}