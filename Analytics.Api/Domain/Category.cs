namespace Analytics.Api.Domain;

public class Category : BaseEntity
{
    public string? Name { get; set; }
    public int Count { get; set; }
}