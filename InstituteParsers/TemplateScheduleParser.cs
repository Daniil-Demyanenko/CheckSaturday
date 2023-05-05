using System;
using System.Collections.Generic;
using System.Globalization;
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
    public List<StudyGroup>? StudyGroups;


    // Позиции групп в расписании, не по порядку, исключая удалённые специализации и т.д. 
    protected List<int> _GroupNamePositions;
    protected int _GroupNameRow = 5; // Строка с названиями групп
    protected CellPosition _FirstVisibleCell;
    protected Workbook _Workbook;
    protected Worksheet _Sheet;
    protected int _MaxDataCol;
    protected int _MaxDataRow;

    public TemplateScheduleParser(string path)
    {
        _Workbook = new Workbook(path);
        _Sheet = FindPageWithSchedule();
        _FirstVisibleCell = GetFirstVisibleCell();
        _MaxDataCol = _Sheet.Cells.MaxDataColumn;
        _MaxDataRow = _Sheet.Cells.MaxDataRow;
    }
    ~TemplateScheduleParser()
    {
        this.Dispose();
    }
    public void Dispose()
    {
        _Sheet.Dispose();
        _Workbook.Dispose();
    }

    public List<ClassInfo> Parse()
    {
        List<ClassInfo> result = new();

        _GroupNamePositions = GetGroupNamePositions();
        var dayPos = GetDaysRowInformation();

        foreach (var pos in _GroupNamePositions)
            result.AddRange(GetGroupClasses(pos, dayPos));

        StudyGroups = GetStudyGroups(_GroupNamePositions);

        return result;
    }

    private List<StudyGroup> GetStudyGroups(List<int> groupsPos)
    {
        List<StudyGroup> result = new();

        foreach (var pos in groupsPos)
        {
            var (course, groupName) = SplitGroupNameForMergedCellsOrNot(colWithGroup: pos);

            result.Add(new StudyGroup(course, groupName));
        }

        return result;
    }

    /// <summary>
    /// Возвращает позиции столбцов с названиями групп
    /// </summary>
    protected virtual List<int> GetGroupNamePositions()
    {
        List<int> result = new();

        for (int i = _FirstVisibleCell.Col; i < _MaxDataCol; i++)
        {
            var cellValue = _Sheet.Cells[_GroupNameRow + 1, i].Value?.ToString()?.Trim();

            if (cellValue is not null && !_Sheet.Cells.Columns[i].IsHidden)       //Ячейка имеет значение, не является скрытой
            {
                if (_Sheet.Cells[_GroupNameRow, i].Value?.ToString().Trim() == "группа") break;
                result.Add(i);
            }
        }

        return result;
    }

    /// <summary>
    /// Возвращает информацию о не скрытых днях недели в расписании
    /// </summary>
    protected List<DayData> GetDaysRowInformation()
    {
        List<DayData> dayPosition = new();

        for (int i = _FirstVisibleCell.Row; i < _MaxDataRow; i++)
        {
            var cellValue = _Sheet.Cells[i, 0].Value?.ToString()?.Trim();

            if (cellValue is not null &&        //Ячейка имеет значение, содержит день недели, не является скрытой
                IsContainDayOfWeek(cellValue) &&
                !_Sheet.Cells.Rows[i].IsHidden)
            {
                CultureInfo culture = new("ru-RU");
                string dateStr = _Sheet.Cells[i, 1].Value?.ToString()?.Trim();
                DateTime date = DateTime.Parse(dateStr, culture);
                dayPosition.Add(new DayData(pos: i, name: cellValue, date: date));
            }
        }

        return dayPosition;
    }

    /// <summary>
    /// Возвращает информацию о парах для определённой группы
    /// </summary>
    /// <param name="col">колонка с группой</param>
    /// <param name="dayPos">Позиции дней недели</param>
    protected virtual List<ClassInfo> GetGroupClasses(int col, List<DayData> dayPos)
    {
        var result = new List<ClassInfo>();
        int course;
        string groupName;

        (course, groupName) = SplitGroupNameForMergedCellsOrNot(colWithGroup: col);

        foreach (var day in dayPos)
        {
            int ClassesCountOfDay = _Sheet.Cells[day.Pos, 0].GetMergedRange().RowCount;
            for (int i = 0; i < ClassesCountOfDay; i++)
            {
                int rowWithCouple = day.Pos + i;
                if (_Sheet.Cells.Rows[rowWithCouple].IsHidden) continue;
                string? className = _Sheet.Cells[rowWithCouple, col].Value?.ToString() ?? null;
                if (className is null) continue;

                var time = _Sheet.Cells[rowWithCouple, 2].Value.ToString().Trim();
                var classItem = new ClassInfo(className, day.Date, day.Name, time, groupName, course);
                result.Add(classItem);
            }
        }

        return result;
    }

    /// <summary>
    /// Разделяет название группы на Курс и Направление подготовки
    /// </summary>
    /// <param name="colWithGroup">индекс колонки с названием группы</param>
    /// <returns>(Курс, Направление подготовки)</returns>
    protected (int, string) SplitGroupName(int colWithGroup)
    {
        string[] groupTitle = _Sheet.Cells[_GroupNameRow, colWithGroup].Value.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries); // Полное название группы
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
    protected (int, string) SplitGroupNameForMerged(int colWithGroup)
    {
        bool isSecondCell = _Sheet.Cells[_GroupNameRow, colWithGroup].Value == null; // Является второй ячейкай в объединении?

        if (isSecondCell) colWithGroup--;

        (int course, string groupName) = SplitGroupName(colWithGroup);

        string appendName = _Sheet.Cells[_GroupNameRow + 1, isSecondCell ? colWithGroup + 1 : colWithGroup].Value.ToString().Trim()[0..^1].Trim();
        groupName += $" [{appendName}]";


        return (course, groupName);
    }


    // Возвращает ячейку, с которой начинается само расписание (без шапки, скрытых строк/столбцов)
    protected CellPosition GetFirstVisibleCell()
    {
        int colCount = 1000;

        for (int row = 7; row < 1000; row++)
        {
            if (_Sheet.Cells.Rows[row].IsHidden) continue;
            for (int col = 3; col < colCount; col++)
            {
                if (_Sheet.Cells.Columns[col].IsHidden) continue;
                Cell cell = _Sheet.Cells[row, col];

                int visibleRow = row;
                int visibleCol = col;

                return new CellPosition(visibleCol, visibleRow);
            }
        }

        throw new Exception("Bad Exel file");
    }


    // Ищем нужную страницу в файле расписания
    protected Worksheet FindPageWithSchedule()
    {
        for (int i = 0; i < _Workbook.Worksheets.Count; i++)
        {
            var sheet = _Workbook.Worksheets[i];
            if (sheet.Cells.MaxDataColumn >= 10 && sheet.Cells.MaxDataRow >= 7) return sheet;
            sheet.Dispose();
        }
        throw new Exception($"Не найдено страницы с расписанием в файле {_Workbook.AbsolutePath}");
    }

    /// <summary>
    /// Строка является названием дня недели?
    /// </summary>
    public static bool IsContainDayOfWeek(string str)
    {
        var days = new string[] { "понедельник", "вторник", "среда", "четверг", "пятница", "суббота" };
        foreach (var day in days)
            if (str.Contains(day, StringComparison.InvariantCultureIgnoreCase)) return true;

        return false;
    }

    /// <summary>
    /// Разделяет название группы на Курс и Направление подготовки.
    /// Добавляет уточнение для объеденённых групп в название
    /// </summary>
    /// <param name="colWithGroup">индекс колонки с названием группы</param>
    /// <returns>(Курс, Направление подготовки)</returns>
    private (int, string) SplitGroupNameForMergedCellsOrNot(int colWithGroup)
    {
        int course;
        string groupName;

        if (_Sheet.Cells[_GroupNameRow, colWithGroup].IsMerged)
            (course, groupName) = SplitGroupNameForMerged(colWithGroup);
        else (course, groupName) = SplitGroupName(colWithGroup);

        return (course, groupName);
    }


    /// <summary>
    /// Положение и название дня недели
    /// </summary>
    protected record DayData
    {
        /// <summary>
        /// Столбец группы
        /// </summary>
        public int Pos;
        public string Name;
        public DateTime Date;
        public DayData(int pos, string name, DateTime date) => (Pos, Name, Date) = (pos, name, date);

    }
}

