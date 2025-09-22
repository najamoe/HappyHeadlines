using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.controllers
{
    [Route("article")]
    [ApiController]
    public class ArticleController : ControllerBase
    {
        private readonly AfricaDbContext _africa;
        private readonly AsiaDbContext _asia;
        private readonly EuropeDbContext _europe;
        private readonly NorthAmericaDbContext _northAmerica;
        private readonly SouthAmericaDbContext _southAmerica;
        private readonly OceaniaDbContext _oceania;
        private readonly AntarcticaDbContext _antarctica;
        private readonly GlobalDbContext _global;

        public ArticleController(
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

        // Helper to select DbContext based on continent
        private DbContext GetDbContext(string continent)
        {
            return continent.ToLower() switch
            {
                "africa" => _africa,
                "asia" => _asia,
                "europe" => _europe,
                "northamerica" => _northAmerica,
                "southamerica" => _southAmerica,
                "oceania" => _oceania,
                "antarctica" => _antarctica,
                "global" => _global,
                _ => throw new Exception("Unknown continent")
            };
        }

        [HttpGet("by-continent/{continent}")]
        public IActionResult GetAllByContinent([FromRoute] string continent)
        {
            try
            {
                var db = GetDbContext(continent);
                var articles = db.Set<Article>().ToList();
                return Ok(articles);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpGet("{id}")]
        public IActionResult GetById([FromQuery] string continent, [FromRoute] int id)
        {
            var db = GetDbContext(continent);
            var article = db.Set<Article>().Find(id);
            if (article == null) return NotFound();
            return Ok(article);
        }

        [HttpPost]
        public IActionResult Create([FromQuery] string continent, [FromBody] Article article)
        {
            if (article == null) return BadRequest();

            var db = GetDbContext(continent);
            db.Set<Article>().Add(article);
            db.SaveChanges();

            return CreatedAtAction(nameof(GetById), new { id = article.Id, continent }, article);
        }

        [HttpPut("{id}")]
        public IActionResult Update([FromQuery] string continent, [FromRoute] int id, [FromBody] Article updatedArticle)
        {
            if (updatedArticle == null || updatedArticle.Id != id) return BadRequest();

            var db = GetDbContext(continent);
            var existingArticle = db.Set<Article>().Find(id);
            if (existingArticle == null) return NotFound();

            existingArticle.Author = updatedArticle.Author;
            existingArticle.Title = updatedArticle.Title;
            existingArticle.Content = updatedArticle.Content;

            db.SaveChanges();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete([FromQuery] string continent, [FromRoute] int id)
        {
            var db = GetDbContext(continent);
            var article = db.Set<Article>().Find(id);
            if (article == null) return NotFound();

            db.Set<Article>().Remove(article);
            db.SaveChanges();
            return NoContent();
        }
    }
}
