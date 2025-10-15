using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsWebsite.Data;
using NewsWebsite.Models;

namespace NewsWebsite.Services.Articles
{
    public class ArticlesService : IArticlesService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ArticlesService> _logger;

        public ArticlesService(ApplicationDbContext dbContext, IHttpClientFactory httpClientFactory, ILogger<ArticlesService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _httpClient = (httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory)))
                .CreateClient("ExternalNewsApi");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Article> CreateArticleAsync(CreateArticleRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var article = new Article
            {
                Title = request.Title,
                Content = request.Content,
                Author = string.IsNullOrWhiteSpace(request.Author) ? "Redacción" : request.Author!,
                PublishedAt = request.PublishedAt ?? DateTime.UtcNow,
                ImageUrl = request.ImageUrl ?? string.Empty,
                RelevanceScore = request.RelevanceScore,
                IsPremium = request.IsPremium
            };

            var categoriesToAttach = new List<Category>();

            if (request.CategoryIds.Any())
            {
                var categoriesById = await _dbContext.Categories
                    .Where(c => request.CategoryIds.Contains(c.Id))
                    .ToListAsync(cancellationToken);
                categoriesToAttach.AddRange(categoriesById);
            }

            if (request.CategoryNames.Any())
            {
                var distinctNames = request.CategoryNames
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (distinctNames.Count > 0)
                {
                    var normalizedNames = distinctNames
                        .Select(name => name.ToLowerInvariant())
                        .ToList();

                    var existingCategories = await _dbContext.Categories
                        .Where(c => normalizedNames.Contains(c.Name.ToLower()))
                        .ToListAsync(cancellationToken);

                    categoriesToAttach.AddRange(existingCategories);

                    var missingNames = distinctNames
                        .Except(existingCategories.Select(c => c.Name), StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    foreach (var name in missingNames)
                    {
                        var category = new Category
                        {
                            Name = name,
                            Description = string.Empty
                        };

                        _dbContext.Categories.Add(category);
                        categoriesToAttach.Add(category);
                    }
                }
            }

            if (categoriesToAttach.Count > 0)
            {
                foreach (var category in categoriesToAttach.DistinctBy(c => c.Id != 0 ? c.Id.ToString() : c.Name))
                {
                    article.Categories.Add(category);
                }
            }

            _dbContext.Articles.Add(article);
            await _dbContext.SaveChangesAsync(cancellationToken);

            try
            {
                await SendToExternalAsync(article, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to notify external API for article {ArticleId}", article.Id);
            }

            return article;
        }

        private Task SendToExternalAsync(Article article, CancellationToken cancellationToken)
        {
            var payload = new
            {
                article.Id,
                article.Title,
                article.Author,
                article.PublishedAt,
                Categories = article.Categories.Select(c => c.Name),
                article.RelevanceScore,
                article.IsPremium
            };

            return _httpClient.PostAsJsonAsync("/news", payload, cancellationToken);
        }
    }
}
