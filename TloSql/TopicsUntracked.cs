using System;
using System.Collections.Generic;

namespace TloSql;

public partial class TopicsUntracked
{
    public int Id { get; set; }

    public int? ForumId { get; set; }

    public string? Name { get; set; }

    public string? InfoHash { get; set; }

    public int? Seeders { get; set; }

    public long? Size { get; set; }

    public int? Status { get; set; }

    public int? RegTime { get; set; }
}
