using System;
using System.Collections.Generic;

namespace NewsWebsite.Models.ViewModels
{
    public class ArticleDetailViewModel
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Content { get; init; } = string.Empty;
        public string Author { get; init; } = string.Empty;
        public DateTime PublishedAt { get; init; }
        public string ImageUrl { get; init; } = string.Empty;
        public bool IsPremium { get; init; }
        public IReadOnlyCollection<string> Categories { get; init; } = Array.Empty<string>();
        public IReadOnlyCollection<ArticleSummaryViewModel> RelatedArticles { get; init; } = Array.Empty<ArticleSummaryViewModel>();
    }
}
