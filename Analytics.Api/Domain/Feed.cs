namespace Analytics.Api.Domain;

public class Feed : BaseEntity
{
    public Feed()
    {
        Link = "";
        Title = "";
        FeedType = "";
        Author = "";
        Content = "";
        PubDate = DateTime.Today;
    }

    public string Link { get; set; }
    public string Title { get; set; }
    public string FeedType { get; set; }
    public string Author { get; set; }
    public string Content { get; set; }
    public DateTime PubDate { get; set; }
}