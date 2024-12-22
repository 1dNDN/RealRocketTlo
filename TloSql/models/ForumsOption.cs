using System;
using System.Collections.Generic;

namespace TloSql;

public partial class ForumsOption
{
    public int ForumId { get; set; }

    public int? TopicId { get; set; }

    public int? AuthorId { get; set; }

    public string? AuthorName { get; set; }

    public int? AuthorPostId { get; set; }

    public string? PostIds { get; set; }
}
