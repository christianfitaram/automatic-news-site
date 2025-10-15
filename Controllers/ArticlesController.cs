using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NewsWebsite.Data;
using NewsWebsite.Models;
using Microsoft.AspNetCore.Authorization;

namespace NewsWebsite.Controllers
{
    public class ArticlesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ArticlesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Articles
        public async Task<IActionResult> Index()
        {
            var articles = await _context.Articles
                .Include(a => a.Categories)
                .ToListAsync();
            return View(articles);
        }

        // GET: Articles/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.Articles
                .Include(a => a.Categories)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (article == null)
            {
                return NotFound();
            }

            return View(article);
        }

        // GET: Articles/Create
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Create()
        {
            await SetCategoryOptionsAsync();
            return View(new Article());
        }

        // POST: Articles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Create(Article article, int[] selectedCategoryIds)
        {
            selectedCategoryIds ??= Array.Empty<int>();

            if (ModelState.IsValid)
            {
                article.Categories = await GetCategoriesByIdsAsync(selectedCategoryIds);
                _context.Add(article);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            article.Categories = await GetCategoriesByIdsAsync(selectedCategoryIds);
            await SetCategoryOptionsAsync(selectedCategoryIds);
            return View(article);
        }

        // GET: Articles/Edit/5
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.Articles
                .Include(a => a.Categories)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (article == null)
            {
                return NotFound();
            }
            await SetCategoryOptionsAsync(article.Categories.Select(c => c.Id));
            return View(article);
        }

        // POST: Articles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Edit(int id, Article article, int[] selectedCategoryIds)
        {
            if (id != article.Id)
            {
                return NotFound();
            }

            selectedCategoryIds ??= Array.Empty<int>();
            var existingArticle = await _context.Articles
                .Include(a => a.Categories)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (existingArticle == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                existingArticle.Title = article.Title;
                existingArticle.Content = article.Content;
                existingArticle.Author = article.Author;
                existingArticle.PublishedAt = article.PublishedAt;
                existingArticle.ImageUrl = article.ImageUrl;
                existingArticle.RelevanceScore = article.RelevanceScore;
                existingArticle.IsPremium = article.IsPremium;

                existingArticle.Categories.Clear();
                var categories = await GetCategoriesByIdsAsync(selectedCategoryIds);
                foreach (var category in categories)
                {
                    existingArticle.Categories.Add(category);
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ArticleExists(existingArticle.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            article.Categories = await GetCategoriesByIdsAsync(selectedCategoryIds);
            await SetCategoryOptionsAsync(selectedCategoryIds);
            return View(article);
        }

        // GET: Articles/Delete/5
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var article = await _context.Articles
                .Include(a => a.Categories)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (article == null)
            {
                return NotFound();
            }

            return View(article);
        }

        // POST: Articles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Editor")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var article = await _context.Articles
                .Include(a => a.Categories)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (article != null)
            {
                _context.Articles.Remove(article);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ArticleExists(int id)
        {
            return _context.Articles.Any(e => e.Id == id);
        }

        private async Task SetCategoryOptionsAsync(IEnumerable<int>? selectedIds = null)
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewData["CategoryOptions"] = new MultiSelectList(categories, "Id", "Name", selectedIds);
        }

        private Task<List<Category>> GetCategoriesByIdsAsync(IEnumerable<int> selectedCategoryIds)
        {
            var ids = selectedCategoryIds?.ToList() ?? new List<int>();
            return _context.Categories
                .Where(c => ids.Contains(c.Id))
                .ToListAsync();
        }
    }
}
