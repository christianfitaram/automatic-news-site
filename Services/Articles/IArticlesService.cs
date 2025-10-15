using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NewsWebsite.Models;

namespace NewsWebsite.Services.Articles
{
    public class CreateArticleRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Author { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? ImageUrl { get; set; }
        public int RelevanceScore { get; set; } = 1;
        public bool IsPremium { get; set; }
        public IReadOnlyCollection<int> CategoryIds { get; set; } = Array.Empty<int>();
        public IReadOnlyCollection<string> CategoryNames { get; set; } = Array.Empty<string>();
    }

    public interface IArticlesService
    {
        Task<Article> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default);
    }
}
