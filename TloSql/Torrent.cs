using System;
using System.Collections.Generic;

namespace TloSql;

public partial class Torrent
{
    public string InfoHash { get; set; } = null!;

    public int ClientId { get; set; }

    public string? TopicId { get; set; }

    public string? Name { get; set; }

    public long? TotalSize { get; set; }

    public bool? Paused { get; set; }

    public double? Done { get; set; }

    public int? TimeAdded { get; set; }

    public bool? Error { get; set; }

    public string? TrackerError { get; set; }
}
