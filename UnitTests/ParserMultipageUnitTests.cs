using System.Collections.Generic;
using CheckSaturday.InstituteParsers;
using Xunit;

namespace CheckSaturday.UnitTests;

public class ParserMultipageUnitTests
{
    [Fact]
    public void ParsingTestZFOMAG_Multipage()
    {
        Assert.True(GetStudyGroups(Paths.ZFOMAG).Count > 0);
    }
    
    private static List<StudyGroup> GetStudyGroups(string path)
    {
        using var parser = new TemplateScheduleParser(path);
        return parser.StudyGroups;
    }
}