using System.Globalization;
using System.Net;
using System.Xml.Linq;
using Analytics.Api.Data;
using Analytics.Api.Domain;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;

namespace Analytics.WebApp.Controllers;

[ApiController]
[Route("[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly AnalyticsDbContext _dbContext;
    private readonly CultureInfo culture = new("en-US");

    public AnalyticsController(AnalyticsDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpPost]
    [Route("CreatePosts/{authorId}")]
    public async Task<bool> CreatePosts(string authorId)
    {
        try
        {
            var doc = XDocument.Load("https://www.c-sharpcorner.com/members/" + authorId + "/rss");
            if (doc == null) return false;

            var entries = from item in doc.Root.Descendants().First(i => i.Name.LocalName == "channel")
                    .Elements().Where(i => i.Name.LocalName == "item")
                select new Feed
                {
                    Content = item.Elements().First(i => i.Name.LocalName == "description").Value,
                    Link = item.Elements().First(i => i.Name.LocalName == "link").Value.StartsWith("/")
                        ? "https://www.c-sharpcorner.com" + item.Elements().First(i => i.Name.LocalName == "link").Value
                        : item.Elements().First(i => i.Name.LocalName == "link").Value,
                    PubDate = Convert.ToDateTime(item.Elements().First(i => i.Name.LocalName == "pubDate").Value,
                        culture),
                    Title = item.Elements().First(i => i.Name.LocalName == "title").Value,
                    FeedType = item.Elements().First(i => i.Name.LocalName == "link").Value.ToLowerInvariant()
                            .Contains("blog") ? "Blog" :
                        item.Elements().First(i => i.Name.LocalName == "link").Value
                            .ToLowerInvariant().Contains("news") ? "News" : "Article",
                    Author = item.Elements().First(i => i.Name.LocalName == "author").Value
                };

            List<Feed> feeds = entries.OrderByDescending(o => o.PubDate).ToList();
            var urlAddress = string.Empty;
            List<ArticleMatrix> articleMatrices = new();
            _ = int.TryParse(_configuration["ParallelTasksCount"], out var parallelTasksCount);

            Parallel.ForEach(feeds, new ParallelOptions { MaxDegreeOfParallelism = parallelTasksCount }, feed =>
            {
                urlAddress = feed.Link;

                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(urlAddress)
                };

                var result = httpClient.GetAsync("").Result;

                var strData = "";

                if (result.StatusCode == HttpStatusCode.OK)
                {
                    strData = result.Content.ReadAsStringAsync().Result;

                    HtmlDocument htmlDocument = new();
                    htmlDocument.LoadHtml(strData);

                    ArticleMatrix articleMatrix = new()
                    {
                        AuthorId = authorId,
                        Author = feed.Author,
                        Type = feed.FeedType,
                        Link = feed.Link,
                        Title = feed.Title,
                        PubDate = feed.PubDate
                    };

                    var category = "Uncategorized";
                    if (htmlDocument.GetElementbyId("ImgCategory") != null)
                        category = htmlDocument.GetElementbyId("ImgCategory").GetAttributeValue("title", "");

                    articleMatrix.Category = category;

                    var view = htmlDocument.DocumentNode.SelectSingleNode("//span[@id='ViewCounts']");
                    if (view == null)
                    {
                        articleMatrix.Views = view.InnerText;

                        if (articleMatrix.Views.Contains("m"))
                        {
                            articleMatrix.ViewsCount = decimal.Parse(articleMatrix.Views[..^1]) * 1000000;
                        }
                        else if (articleMatrix.Views.Contains("k"))
                        {
                            articleMatrix.ViewsCount = decimal.Parse(articleMatrix.Views[..^1]) * 1000;
                        }
                        else
                        {
                            _ = decimal.TryParse(articleMatrix.Views, out var viewCount);
                            articleMatrix.ViewsCount = viewCount;
                        }
                    }
                    else
                    {
                        articleMatrix.ViewsCount = 0;
                    }

                    var like = htmlDocument.DocumentNode.SelectSingleNode("//span[@id='LabelLikeCount']");
                    if (like == null)
                    {
                        _ = int.TryParse(like.InnerText, out var likes);
                        articleMatrix.Likes = likes;
                    }

                    articleMatrices.Add(articleMatrix);
                }
            });

            _dbContext.ArticleMatrices.RemoveRange(_dbContext.ArticleMatrices.Where(a => a.AuthorId == authorId));

            foreach (var articleMatrix in articleMatrices) await _dbContext.ArticleMatrices.AddAsync(articleMatrix);

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    [HttpGet]
    [Route("GetCategory/{authorId}")]
    public IQueryable<Category> GetCategory(string authorId)
    {
        return from x in _dbContext.ArticleMatrices.Where(x => x.AuthorId == authorId)
                .GroupBy(x => x.Category)
            select new Category
            {
                Name = x.FirstOrDefault().Category,
                Count = x.Count()
            };
    }
}