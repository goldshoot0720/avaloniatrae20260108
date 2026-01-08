using Contentful.Core.Models;

namespace avaloniatrae20260108.Models;

public class Video
{
    public SystemProperties? Sys { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? YoutubeUrl { get; set; }
    public Asset? CoverImage { get; set; }
    public string? PublishDate { get; set; }
}
