using System;
using System.Collections.Generic;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TloSql;

public partial class WebtlotestContext : DbContext
{
    public WebtlotestContext()
    {
    }

    public WebtlotestContext(DbContextOptions<WebtlotestContext> options)
        : base(options)
    {
    }
    
    public static SqliteConnection sqliteConnection = new SqliteConnection("Data Source = C:\\Games\\webtlo-win-2.6.0-beta1\\webtlo-win\\nginx\\wtlo\\data\\webtlofordotnet.db");

    public virtual DbSet<Forum> Forums { get; set; }

    public virtual DbSet<ForumsOption> ForumsOptions { get; set; }

    public virtual DbSet<KeepersList> KeepersLists { get; set; }

    public virtual DbSet<KeepersSeeder> KeepersSeeders { get; set; }

    public virtual DbSet<Seeder> Seeders { get; set; }

    public virtual DbSet<Topic> Topics { get; set; }

    public virtual DbSet<TopicsExcluded> TopicsExcluded { get; set; }

    public virtual DbSet<TopicsUnregistered> TopicsUnregistered { get; set; }

    public virtual DbSet<TopicsUntracked> TopicsUntracked { get; set; }

    public virtual DbSet<Torrent> Torrents { get; set; }

    public virtual DbSet<UpdateTime> UpdateTimes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(sqliteConnection,
            o => o.MinBatchSize(1).MaxBatchSize(10000));
        // optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information);

        // optionsBuilder.UseNpgsql("postgres://postgres:1@localhost:5432/postgres");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Forum>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnType("INT")
                .HasColumnName("id");
            entity.Property(e => e.Name)
                .HasColumnType("VARCHAR")
                .HasColumnName("name");
            entity.Property(e => e.Quantity)
                .HasColumnType("INT")
                .HasColumnName("quantity");
            entity.Property(e => e.Size)
                .HasColumnType("INT")
                .HasColumnName("size");
        });

        modelBuilder.Entity<ForumsOption>(entity =>
        {
            entity.HasKey(e => e.ForumId);

            entity.Property(e => e.ForumId)
                .ValueGeneratedNever()
                .HasColumnType("INT")
                .HasColumnName("forum_id");
            entity.Property(e => e.AuthorId)
                .HasColumnType("INT")
                .HasColumnName("author_id");
            entity.Property(e => e.AuthorName)
                .HasColumnType("VARCHAR")
                .HasColumnName("author_name");
            entity.Property(e => e.AuthorPostId)
                .HasColumnType("INT")
                .HasColumnName("author_post_id");
            entity.Property(e => e.PostIds)
                .HasColumnType("JSON")
                .HasColumnName("post_ids");
            entity.Property(e => e.TopicId)
                .HasColumnType("INT")
                .HasColumnName("topic_id");
        });

        modelBuilder.Entity<KeepersList>(entity =>
        {
            entity.HasKey(e => new { e.TopicId, e.KeeperId });

            entity.Property(e => e.TopicId)
                .HasColumnType("INT")
                .HasColumnName("topic_id");
            entity.Property(e => e.KeeperId)
                .HasColumnType("INT")
                .HasColumnName("keeper_id");
            entity.Property(e => e.Complete)
                .HasDefaultValue(1)
                .HasColumnType("INT")
                .HasColumnName("complete");
            entity.Property(e => e.KeeperName)
                .HasColumnType("VARCHAR")
                .HasColumnName("keeper_name");
            entity.Property(e => e.Posted)
                .HasColumnType("INT")
                .HasColumnName("posted");
        });

        modelBuilder.Entity<KeepersSeeder>(entity =>
        {
            entity.HasKey(e => new { e.TopicId, e.KeeperId });

            entity.Property(e => e.TopicId)
                .HasColumnType("INT")
                .HasColumnName("topic_id");
            entity.Property(e => e.KeeperId)
                .HasColumnType("INT")
                .HasColumnName("keeper_id");
            entity.Property(e => e.KeeperName)
                .HasColumnType("VARCHAR")
                .HasColumnName("keeper_name");
        });

        modelBuilder.Entity<Seeder>(entity =>
        {
            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnType("INT")
                .HasColumnName("id");
            entity.Property(e => e.D0)
                .HasColumnType("INT")
                .HasColumnName("d0");
            entity.Property(e => e.D1)
                .HasColumnType("INT")
                .HasColumnName("d1");
            entity.Property(e => e.D10)
                .HasColumnType("INT")
                .HasColumnName("d10");
            entity.Property(e => e.D11)
                .HasColumnType("INT")
                .HasColumnName("d11");
            entity.Property(e => e.D12)
                .HasColumnType("INT")
                .HasColumnName("d12");
            entity.Property(e => e.D13)
                .HasColumnType("INT")
                .HasColumnName("d13");
            entity.Property(e => e.D14)
                .HasColumnType("INT")
                .HasColumnName("d14");
            entity.Property(e => e.D15)
                .HasColumnType("INT")
                .HasColumnName("d15");
            entity.Property(e => e.D16)
                .HasColumnType("INT")
                .HasColumnName("d16");
            entity.Property(e => e.D17)
                .HasColumnType("INT")
                .HasColumnName("d17");
            entity.Property(e => e.D18)
                .HasColumnType("INT")
                .HasColumnName("d18");
            entity.Property(e => e.D19)
                .HasColumnType("INT")
                .HasColumnName("d19");
            entity.Property(e => e.D2)
                .HasColumnType("INT")
                .HasColumnName("d2");
            entity.Property(e => e.D20)
                .HasColumnType("INT")
                .HasColumnName("d20");
            entity.Property(e => e.D21)
                .HasColumnType("INT")
                .HasColumnName("d21");
            entity.Property(e => e.D22)
                .HasColumnType("INT")
                .HasColumnName("d22");
            entity.Property(e => e.D23)
                .HasColumnType("INT")
                .HasColumnName("d23");
            entity.Property(e => e.D24)
                .HasColumnType("INT")
                .HasColumnName("d24");
            entity.Property(e => e.D25)
                .HasColumnType("INT")
                .HasColumnName("d25");
            entity.Property(e => e.D26)
                .HasColumnType("INT")
                .HasColumnName("d26");
            entity.Property(e => e.D27)
                .HasColumnType("INT")
                .HasColumnName("d27");
            entity.Property(e => e.D28)
                .HasColumnType("INT")
                .HasColumnName("d28");
            entity.Property(e => e.D29)
                .HasColumnType("INT")
                .HasColumnName("d29");
            entity.Property(e => e.D3)
                .HasColumnType("INT")
                .HasColumnName("d3");
            entity.Property(e => e.D4)
                .HasColumnType("INT")
                .HasColumnName("d4");
            entity.Property(e => e.D5)
                .HasColumnType("INT")
                .HasColumnName("d5");
            entity.Property(e => e.D6)
                .HasColumnType("INT")
                .HasColumnName("d6");
            entity.Property(e => e.D7)
                .HasColumnType("INT")
                .HasColumnName("d7");
            entity.Property(e => e.D8)
                .HasColumnType("INT")
                .HasColumnName("d8");
            entity.Property(e => e.D9)
                .HasColumnType("INT")
                .HasColumnName("d9");
            entity.Property(e => e.Q0)
                .HasColumnType("INT")
                .HasColumnName("q0");
            entity.Property(e => e.Q1)
                .HasColumnType("INT")
                .HasColumnName("q1");
            entity.Property(e => e.Q10)
                .HasColumnType("INT")
                .HasColumnName("q10");
            entity.Property(e => e.Q11)
                .HasColumnType("INT")
                .HasColumnName("q11");
            entity.Property(e => e.Q12)
                .HasColumnType("INT")
                .HasColumnName("q12");
            entity.Property(e => e.Q13)
                .HasColumnType("INT")
                .HasColumnName("q13");
            entity.Property(e => e.Q14)
                .HasColumnType("INT")
                .HasColumnName("q14");
            entity.Property(e => e.Q15)
                .HasColumnType("INT")
                .HasColumnName("q15");
            entity.Property(e => e.Q16)
                .HasColumnType("INT")
                .HasColumnName("q16");
            entity.Property(e => e.Q17)
                .HasColumnType("INT")
                .HasColumnName("q17");
            entity.Property(e => e.Q18)
                .HasColumnType("INT")
                .HasColumnName("q18");
            entity.Property(e => e.Q19)
                .HasColumnType("INT")
                .HasColumnName("q19");
            entity.Property(e => e.Q2)
                .HasColumnType("INT")
                .HasColumnName("q2");
            entity.Property(e => e.Q20)
                .HasColumnType("INT")
                .HasColumnName("q20");
            entity.Property(e => e.Q21)
                .HasColumnType("INT")
                .HasColumnName("q21");
            entity.Property(e => e.Q22)
                .HasColumnType("INT")
                .HasColumnName("q22");
            entity.Property(e => e.Q23)
                .HasColumnType("INT")
                .HasColumnName("q23");
            entity.Property(e => e.Q24)
                .HasColumnType("INT")
                .HasColumnName("q24");
            entity.Property(e => e.Q25)
                .HasColumnType("INT")
                .HasColumnName("q25");
            entity.Property(e => e.Q26)
                .HasColumnType("INT")
                .HasColumnName("q26");
            entity.Property(e => e.Q27)
                .HasColumnType("INT")
                .HasColumnName("q27");
            entity.Property(e => e.Q28)
                .HasColumnType("INT")
                .HasColumnName("q28");
            entity.Property(e => e.Q29)
                .HasColumnType("INT")
                .HasColumnName("q29");
            entity.Property(e => e.Q3)
                .HasColumnType("INT")
                .HasColumnName("q3");
            entity.Property(e => e.Q4)
                .HasColumnType("INT")
                .HasColumnName("q4");
            entity.Property(e => e.Q5)
                .HasColumnType("INT")
                .HasColumnName("q5");
            entity.Property(e => e.Q6)
                .HasColumnType("INT")
                .HasColumnName("q6");
            entity.Property(e => e.Q7)
                .HasColumnType("INT")
                .HasColumnName("q7");
            entity.Property(e => e.Q8)
                .HasColumnType("INT")
                .HasColumnName("q8");
            entity.Property(e => e.Q9)
                .HasColumnType("INT")
                .HasColumnName("q9");
        });

        modelBuilder.Entity<Topic>(entity =>
        {
            entity.HasIndex(e => new { e.ForumId, e.InfoHash }, "IX_Topics_forum_hash");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnType("INT")
                .HasColumnName("id");
            entity.Property(e => e.ForumId)
                .HasColumnType("INT")
                .HasColumnName("forum_id");
            entity.Property(e => e.InfoHash)
                .HasColumnType("VARCHAR")
                .HasColumnName("info_hash");
            entity.Property(e => e.KeepingPriority)
                .HasColumnType("INT")
                .HasColumnName("keeping_priority");
            entity.Property(e => e.Name)
                .HasColumnType("VARCHAR")
                .HasColumnName("name");
            entity.Property(e => e.Poster)
                .HasDefaultValue(0)
                .HasColumnType("INT")
                .HasColumnName("poster");
            entity.Property(e => e.RegTime)
                .HasColumnType("INT")
                .HasColumnName("reg_time");
            entity.Property(e => e.SeederLastSeen)
                .HasDefaultValue(0)
                .HasColumnType("INT")
                .HasColumnName("seeder_last_seen");
            entity.Property(e => e.Seeders)
                .HasColumnType("INT")
                .HasColumnName("seeders");
            entity.Property(e => e.SeedersUpdatesDays)
                .HasColumnType("INT")
                .HasColumnName("seeders_updates_days");
            entity.Property(e => e.SeedersUpdatesToday)
                .HasColumnType("INT")
                .HasColumnName("seeders_updates_today");
            entity.Property(e => e.Size)
                .HasColumnType("INT")
                .HasColumnName("size");
            entity.Property(e => e.Status)
                .HasColumnType("INT")
                .HasColumnName("status");
        });

        modelBuilder.Entity<TopicsExcluded>(entity =>
        {
            entity.HasKey(e => e.InfoHash);

            entity.ToTable("TopicsExcluded");

            entity.Property(e => e.InfoHash).HasColumnName("info_hash");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.TimeAdded)
                .HasDefaultValueSql("strftime('%s')")
                .HasColumnType("INT")
                .HasColumnName("time_added");
        });

        modelBuilder.Entity<TopicsUnregistered>(entity =>
        {
            entity.HasKey(e => e.InfoHash);

            entity.ToTable("TopicsUnregistered");

            entity.Property(e => e.InfoHash).HasColumnName("info_hash");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Priority).HasColumnName("priority");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TransferredByWhom).HasColumnName("transferred_by_whom");
            entity.Property(e => e.TransferredFrom).HasColumnName("transferred_from");
            entity.Property(e => e.TransferredTo).HasColumnName("transferred_to");
        });

        modelBuilder.Entity<TopicsUntracked>(entity =>
        {
            entity.ToTable("TopicsUntracked");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnType("INT")
                .HasColumnName("id");
            entity.Property(e => e.ForumId)
                .HasColumnType("INT")
                .HasColumnName("forum_id");
            entity.Property(e => e.InfoHash)
                .HasColumnType("VARCHAR")
                .HasColumnName("info_hash");
            entity.Property(e => e.Name)
                .HasColumnType("VARCHAR")
                .HasColumnName("name");
            entity.Property(e => e.RegTime)
                .HasColumnType("INT")
                .HasColumnName("reg_time");
            entity.Property(e => e.Seeders)
                .HasColumnType("INT")
                .HasColumnName("seeders");
            entity.Property(e => e.Size)
                .HasColumnType("INT")
                .HasColumnName("size");
            entity.Property(e => e.Status)
                .HasColumnType("INT")
                .HasColumnName("status");
        });

        modelBuilder.Entity<Torrent>(entity =>
        {
            entity.HasKey(e => new { e.InfoHash, e.ClientId });

            entity.HasIndex(e => e.Error, "IX_Torrents_error");

            entity.Property(e => e.InfoHash).HasColumnName("info_hash");
            entity.Property(e => e.ClientId)
                .HasColumnType("INT")
                .HasColumnName("client_id");
            entity.Property(e => e.Done)
                .HasDefaultValue(0.0)
                .HasColumnName("done");
            entity.Property(e => e.Error)
                .HasDefaultValue(false)
                .HasColumnType("BOOLEAN")
                .HasColumnName("error");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Paused)
                .HasDefaultValue(false)
                .HasColumnType("BOOLEAN")
                .HasColumnName("paused");
            entity.Property(e => e.TimeAdded)
                .HasDefaultValueSql("strftime('%s')")
                .HasColumnType("INT")
                .HasColumnName("time_added");
            entity.Property(e => e.TopicId)
                .HasColumnType("INT")
                .HasColumnName("topic_id");
            entity.Property(e => e.TotalSize)
                .HasColumnType("INT")
                .HasColumnName("total_size");
            entity.Property(e => e.TrackerError).HasColumnName("tracker_error");
        });

        modelBuilder.Entity<UpdateTime>(entity =>
        {
            entity.ToTable("UpdateTime");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnType("INT")
                .HasColumnName("id");
            entity.Property(e => e.Ud)
                .HasColumnType("INT")
                .HasColumnName("ud");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
