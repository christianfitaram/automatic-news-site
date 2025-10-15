using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NewsWebsite.Models
{
    public class Article
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string Author { get; set; } = "Redacción";

        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;

        public List<Category> Categories { get; set; } = new();

        public string ImageUrl { get; set; } = "";

        public int RelevanceScore { get; set; } = 1;

        public bool IsPremium { get; set; } = false;
    }
}
