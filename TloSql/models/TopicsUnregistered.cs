using System;
using System.Collections.Generic;

namespace TloSql;

public partial class TopicsUnregistered
{
    public string InfoHash { get; set; } = null!;

    public string? Name { get; set; }

    public string Status { get; set; } = null!;

    public string? Priority { get; set; }

    public string? TransferredFrom { get; set; }

    public string? TransferredTo { get; set; }

    public string? TransferredByWhom { get; set; }
}
