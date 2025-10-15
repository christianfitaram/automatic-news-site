using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NewsWebsite.Data;
using NewsWebsite.Models;
using NewsWebsite.Models.ViewModels;
using NewsWebsite.Extensions;

namespace NewsWebsite.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _dbContext;

    public HomeController(ApplicationDbContext dbContext, ILogger<HomeController> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IActionResult> Index()
    {
        var recentArticles = await _dbContext.Articles
            .AsNoTracking()
            .Include(a => a.Categories)
            .OrderByDescending(a => a.PublishedAt)
            .ThenByDescending(a => a.Id)
            .Take(9)
            .ToListAsync();

        var featured = recentArticles.FirstOrDefault();
        var latestArticles = recentArticles.Skip(1)
            .Select(article => article.ToSummaryViewModel(180))
            .ToList();

        var categories = await _dbContext.Categories
            .AsNoTrackingWithIdentityResolution()
            .Include(c => c.Articles)
                .ThenInclude(a => a.Categories)
            .OrderBy(c => c.Name)
            .ToListAsync();

        var categorySections = categories
            .Select(category => new CategorySectionViewModel
            {
                CategoryId = category.Id,
                Name = category.Name,
                Description = category.Description,
                Articles = category.Articles
                    .OrderByDescending(article => article.PublishedAt)
                    .ThenByDescending(article => article.Id)
                    .Take(4)
                    .Select(article => article.ToSummaryViewModel(140))
                    .ToList()
            })
            .Where(section => section.Articles.Count > 0)
            .ToList();

        var model = new HomeIndexViewModel
        {
            FeaturedArticle = featured != null ? featured.ToSummaryViewModel(260) : null,
            LatestArticles = latestArticles,
            CategorySections = categorySections
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

}
