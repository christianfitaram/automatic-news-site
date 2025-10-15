using System.Collections.Generic;

namespace NewsWebsite.Models.ViewModels
{
    public class ArticlesIndexViewModel
    {
        public string Query { get; init; } = string.Empty;
        public int? SelectedCategoryId { get; init; }
        public IReadOnlyCollection<ArticleSummaryViewModel> Articles { get; init; } = new List<ArticleSummaryViewModel>();
        public IReadOnlyCollection<CategoryOptionViewModel> Categories { get; init; } = new List<CategoryOptionViewModel>();
    }

    public class CategoryOptionViewModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int ArticleCount { get; init; }
    }
}
