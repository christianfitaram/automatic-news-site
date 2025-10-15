using System.Collections.Generic;

namespace NewsWebsite.Models.ViewModels
{
    public class CategoryDetailViewModel
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public IReadOnlyCollection<ArticleSummaryViewModel> Articles { get; init; } = Array.Empty<ArticleSummaryViewModel>();
    }
}
