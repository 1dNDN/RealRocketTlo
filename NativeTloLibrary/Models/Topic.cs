namespace NativeTloLibrary.Models;

public struct Topic
{
    public long TopicId { get; set; }

    public long ForumId { get; set; }

    public string Name { get; set; }

    public string InfoHash { get; set; }

    public long Seeders { get; set; }

    public long TorSizeBytes { get; set; }

    public long TorStatus { get; set; }

    public long RegTime { get; set; }

    public long SeedersUpdatesToday { get; set; }

    public long SeedersUpdatesDays { get; set; }

    public long KeepingPriority { get; set; }

    public long TopicPoster { get; set; }

    public long SeederLastSeen { get; set; }
}
