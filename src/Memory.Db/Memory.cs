namespace Memory.Db;

public class Memory {
    public long Id { get; set; }

    public required string FilePath { get; set; }
    public required string FilePathHash { get; set; }
    public required string FileExtension { get; set; }
    public required long FileSize { get; set; }

    // This is only used for small file
    public string? FileContentHash { get; set; }

    public DateTime CreationTime { get; set; }

    public int Year { get; private set; }
    public int Month { get; private set; }
    public int Day { get; private set; }

    public int Likes { get; set; }
    public int Views { get; set; }

    public bool IsTaggedByFace { get; set; }

    public MemoryMeta? MemoryMeta { get; set; }
    public List<MemoryTag> MemoryTags { get; set; } = [];
}

public class MemoryMeta {
    public long Id { get; set; }
    public long MemoryId { get; set; }

    public DateTime? DateTimeOriginal { get; set; }
    public string? OffsetTimeOriginal { get; set; }
    public string? Make { get; set; }
    public string? Modal { get; set; }
    public string? LensModal { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public Memory? Memory { get; set; }
}

public class Tag {
    public int Id { get; set; }
    public required string Name { get; set; }

    public List<MemoryTag> MemoryTags { get; set; } = [];
}

public class MemoryTag {
    public int TagId { get; set; }
    public long MemoryId { get; set; }

    public Tag? Tag { get; set; }
    public Memory? Memory { get; set; }
}
