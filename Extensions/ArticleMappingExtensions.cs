using System;
using System.Collections.Generic;
using System.Linq;
using NewsWebsite.Models;
using NewsWebsite.Models.ViewModels;

namespace NewsWebsite.Extensions
{
    public static class ArticleMappingExtensions
    {
        public static ArticleSummaryViewModel ToSummaryViewModel(this Article article, int excerptLength)
        {
            if (article == null)
            {
                throw new ArgumentNullException(nameof(article));
            }

            return new ArticleSummaryViewModel
            {
                Id = article.Id,
                Title = article.Title,
                Author = string.IsNullOrWhiteSpace(article.Author) ? "Redacción" : article.Author,
                PublishedAt = article.PublishedAt,
                ImageUrl = string.IsNullOrWhiteSpace(article.ImageUrl) ? string.Empty : article.ImageUrl,
                IsPremium = article.IsPremium,
                Excerpt = BuildExcerpt(article.Content, excerptLength),
                Categories = article.Categories
                    .Select(category => category.Name)
                    .ToList()
            };
        }

        public static ArticleDetailViewModel ToDetailViewModel(this Article article, IEnumerable<ArticleSummaryViewModel> related)
        {
            if (article == null)
            {
                throw new ArgumentNullException(nameof(article));
            }

            return new ArticleDetailViewModel
            {
                Id = article.Id,
                Title = article.Title,
                Content = article.Content,
                Author = string.IsNullOrWhiteSpace(article.Author) ? "Redacción" : article.Author,
                PublishedAt = article.PublishedAt,
                ImageUrl = string.IsNullOrWhiteSpace(article.ImageUrl) ? string.Empty : article.ImageUrl,
                IsPremium = article.IsPremium,
                Categories = article.Categories
                    .Select(category => category.Name)
                    .ToList(),
                RelatedArticles = related?.ToList() ?? new List<ArticleSummaryViewModel>()
            };
        }

        private static string BuildExcerpt(string? content, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var normalized = content.Trim();
            if (normalized.Length <= maxLength)
            {
                return normalized;
            }

            var excerpt = normalized[..Math.Min(normalized.Length, maxLength)];
            var lastSpaceIndex = excerpt.LastIndexOf(' ');
            if (lastSpaceIndex > 0 && lastSpaceIndex > maxLength * 0.6)
            {
                excerpt = excerpt[..lastSpaceIndex];
            }

            return $"{excerpt}…";
        }
    }
}
