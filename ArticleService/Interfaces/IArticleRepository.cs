using ArticleService.Models;

namespace ArticleService.Interfaces
{
    public interface IArticleRepository
    {
        Task<IEnumerable<Article>> GetRecentArticlesAsync(int days);
        Task<Article?> GetArticleByIdAsync(string id);
        Task CreateArticleAsync(Article article);
    }
}
