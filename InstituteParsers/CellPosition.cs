using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CheckSaturday.InstituteParsers;

public record CellPosition(int Col, int Row)
{
    public int Col = Col;
    public int Row = Row;
}