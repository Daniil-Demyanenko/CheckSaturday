using System;
using System.Collections.Generic;
using System.Linq;
using CheckSaturday.InstituteParsers;
using Xunit;



namespace CheckSaturday.UnitTests
{
    public class ParserStudyGroupsUnitTests
    {
        [Fact]
        public void ParsingTestOFOBAK1_GroupCountInStudyGroups()
        {
            var actual = GetStudyGroups(Paths.OFOBAK1).Count;

            Assert.Equal(43, actual);
        }


        public void ParsingTestOFOMAG_GroupCountInStudyGroups()
        {
            var actual = GetStudyGroups(Paths.OFOMAG).Count;

            Assert.Equal(17, actual);
        }

        [Fact]
        public void ParsingTestZFOMAG_GroupCountInStudyGroups()
        {
            var actual = GetStudyGroups(Paths.ZFOMAG).Count;

            Assert.Equal(4, actual);
        }



        public static List<StudyGroup> GetStudyGroups(string path)
        {
            using var parser = new TemplateScheduleParser(path);
            parser.Parse();
            var couples = parser.StudyGroups;
            return couples.ToList();
        }
    }
}