using System;
using System.Collections.Generic;

namespace TloSql;

public partial class KeepersList
{
    public int TopicId { get; set; }

    public int KeeperId { get; set; }

    public string? KeeperName { get; set; }

    public int? Posted { get; set; }

    public int? Complete { get; set; }
}
