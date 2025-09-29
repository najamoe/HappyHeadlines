using Microsoft.AspNetCore.Mvc;
using DraftService.Data;
using DraftService.Models;

namespace DraftService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DraftController : ControllerBase
    {
        private readonly DraftDbContext _context;

        public DraftController(DraftDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetAll() => Ok(_context.Drafts.ToList());

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            var draft = _context.Drafts.Find(id);
            if (draft == null) return NotFound();
            return Ok(draft);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Draft draft)
        {
            _context.Drafts.Add(draft);
            _context.SaveChanges();
            return CreatedAtAction(nameof(Get), new { id = draft.Id }, draft);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Draft draft)
        {
            if (id != draft.Id) return BadRequest();
            _context.Drafts.Update(draft);
            _context.SaveChanges();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            var draft = _context.Drafts.Find(id);
            if (draft == null) return NotFound();
            _context.Drafts.Remove(draft);
            _context.SaveChanges();
            return NoContent();
        }
    }
}
