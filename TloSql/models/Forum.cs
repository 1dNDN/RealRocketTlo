using System;
using System.Collections.Generic;

namespace TloSql;

public partial class Forum
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? Quantity { get; set; }

    public long? Size { get; set; }
}
