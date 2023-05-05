using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CheckSaturday.InstituteParsers;

public record StudyGroup
{
    public int Course { get; init; }
    public string GroupName { get; init; }

    public StudyGroup(int course, string groupName)
        => (Course, GroupName) = (course, groupName);
}