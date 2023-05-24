namespace Domain;

// [GenerateSerializer]
// public record UrlDetails
// {
//     [Id(0)] public string FullUrl { get; set; }
//     [Id(1)] public string ShortenedRouteSegment { get; set; }
// }

[GenerateSerializer]
public record UrlDetails(string FullUrl, string ShortenedRouteSegment)
{
    public UrlDetails() : this("", "")
    {
    }
}