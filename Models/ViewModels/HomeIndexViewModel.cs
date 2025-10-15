using System;
using System.Collections.Generic;

namespace NewsWebsite.Models.ViewModels
{
    public class ArticleSummaryViewModel
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Excerpt { get; init; } = string.Empty;
        public string Author { get; init; } = string.Empty;
        public DateTime PublishedAt { get; init; }
        public string ImageUrl { get; init; } = string.Empty;
        public bool IsPremium { get; init; }
        public IReadOnlyCollection<string> Categories { get; init; } = Array.Empty<string>();
    }

    public class CategorySectionViewModel
    {
        public int CategoryId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public IReadOnlyCollection<ArticleSummaryViewModel> Articles { get; init; } = Array.Empty<ArticleSummaryViewModel>();
    }

    public class HomeIndexViewModel
    {
        public ArticleSummaryViewModel? FeaturedArticle { get; init; }
        public IReadOnlyCollection<ArticleSummaryViewModel> LatestArticles { get; init; } = Array.Empty<ArticleSummaryViewModel>();
        public IReadOnlyCollection<CategorySectionViewModel> CategorySections { get; init; } = Array.Empty<CategorySectionViewModel>();
    }
}
