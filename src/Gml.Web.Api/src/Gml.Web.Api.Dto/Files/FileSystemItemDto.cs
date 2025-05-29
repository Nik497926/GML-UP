using System;

namespace Gml.Web.Api.Dto.Files;

public class FileSystemItemDto
{
    public string Name { get; set; } = null!;
    public string Path { get; set; } = null!;
    public string FullPath { get; set; } = null!;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? Extension { get; set; }
} 