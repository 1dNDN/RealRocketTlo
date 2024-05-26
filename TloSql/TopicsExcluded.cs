using System;
using System.Collections.Generic;

namespace TloSql;

public partial class TopicsExcluded
{
    public string InfoHash { get; set; } = null!;

    public int? TimeAdded { get; set; }

    public string? Comment { get; set; }
}
