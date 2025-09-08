using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;

namespace api.controllers
{
    [Route("api/article")]
    [ApiController]
    public class ArticleController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        public ArticleController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var articles = _context.Articles.ToList();

            return Ok(articles);
        }

        [HttpGet("{id}")]
        public IActionResult GetById([FromRoute] int id)
        {
            var article = _context.Articles.Find(id);

            if (article == null)
            {
                return NotFound();
            }

            return Ok(article);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Article article)
        {
            if (article == null)
            {
                return BadRequest();
            }

            _context.Articles.Add(article);
            _context.SaveChanges();

            // Returns 201 Created and the created article
            return CreatedAtAction(nameof(GetById), new { id = article.Id }, article);
        }

        [HttpPut("{id}")]
        public IActionResult Update([FromRoute] int id, [FromBody] Article updatedArticle)
        {
            if (updatedArticle == null || updatedArticle.Id != id)
            {
                return BadRequest();
            }

            var existingArticle = _context.Articles.Find(id);
            if (existingArticle == null)
            {
                return NotFound();
            }

            existingArticle.Author = updatedArticle.Author;
            existingArticle.Title = updatedArticle.Title;
            existingArticle.Content = updatedArticle.Content;

            _context.SaveChanges();

            return NoContent(); // 204 indicates update succeeded but no content returned
        }

        [HttpDelete("{id}")]
        public IActionResult Delete([FromRoute] int id)
        {
            var article = _context.Articles.Find(id);
            if (article == null)
            {
                return NotFound();
            }

            _context.Articles.Remove(article);
            _context.SaveChanges();

            return NoContent(); // 204 indicates deletion succeeded
        }



    }
}