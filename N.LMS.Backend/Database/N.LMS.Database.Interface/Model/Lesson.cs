using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace N.LMS.Database.Interface.Model;

[Index(nameof(ModuleId), nameof(SortOrder))]
public class Lesson
{
    [Key] public Guid LessonId { get; set; } = Guid.NewGuid();
    [Required] public Guid ModuleId { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    public LessonType Type { get; set; } = LessonType.Article;
    [MaxLength(1024)] public string? ContentUrl { get; set; }
    public string? ContentBody { get; set; }
    public int? DurationMinutes { get; set; }
    public int SortOrder { get; set; }
    public bool Deleted { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;

    public Module? Module { get; set; }
}
