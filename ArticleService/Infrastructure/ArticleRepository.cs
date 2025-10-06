using ArticleService.Data;
using ArticleService.Models;
using ArticleService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.Infrastructure
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly AfricaDbContext _africa;
        private readonly AsiaDbContext _asia;
        private readonly EuropeDbContext _europe;
        private readonly NorthAmericaDbContext _northAmerica;
        private readonly SouthAmericaDbContext _southAmerica;
        private readonly OceaniaDbContext _oceania;
        private readonly AntarcticaDbContext _antarctica;
        private readonly GlobalDbContext _global;

        public ArticleRepository(
            AfricaDbContext africa,
            AsiaDbContext asia,
            EuropeDbContext europe,
            NorthAmericaDbContext northAmerica,
            SouthAmericaDbContext southAmerica,
            OceaniaDbContext oceania,
            AntarcticaDbContext antarctica,
            GlobalDbContext global)
        {
            _africa = africa;
            _asia = asia;
            _europe = europe;
            _northAmerica = northAmerica;
            _southAmerica = southAmerica;
            _oceania = oceania;
            _antarctica = antarctica;
            _global = global;
        }

        public async Task<IEnumerable<Article>> GetArticlesFromLast14DaysAsync()
        {
            var since = DateTime.UtcNow.AddDays(-14);

            var africa = await _africa.Articles.Where(a => a.PublishedAt >= since).ToListAsync();
            var asia = await _asia.Articles.Where(a => a.PublishedAt >= since).ToListAsync();
            var europe = await _europe.Articles.Where(a => a.PublishedAt >= since).ToListAsync();
            var na = await _northAmerica.Articles.Where(a => a.PublishedAt >= since).ToListAsync();
            var sa = await _southAmerica.Articles.Where(a => a.PublishedAt >= since).ToListAsync();
            var oceania = await _oceania.Articles.Where(a => a.PublishedAt >= since).ToListAsync();
            var antarctica = await _antarctica.Articles.Where(a => a.PublishedAt >= since).ToListAsync();
            var global = await _global.Articles.Where(a => a.PublishedAt >= since).ToListAsync();

            // Merge all results into one list
            return africa
                .Concat(asia)
                .Concat(europe)
                .Concat(na)
                .Concat(sa)
                .Concat(oceania)
                .Concat(antarctica)
                .Concat(global);
        }
    }
}
