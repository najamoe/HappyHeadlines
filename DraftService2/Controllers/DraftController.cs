using Microsoft.AspNetCore.Mvc;
using DraftService.Data;
using DraftService.Models;
using Monitoring;

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
        public IActionResult GetAll()
        {
            MonitorService.Log.Information("Fetching all drafts");
            var drafts = _context.Drafts.ToList();
            return Ok(drafts);
        }

        [HttpGet("{id}")]
        public IActionResult Get(int id)
        {
            MonitorService.Log.Information("Fetching draft with ID {DraftId}", id);
            var draft = _context.Drafts.Find(id);
            if (draft == null)
            {
                MonitorService.Log.Warning("Draft with ID {DraftId} not found", id);
                return NotFound();
            }
            return Ok(draft);
        }

        [HttpPost]
        public IActionResult Create([FromBody] Draft draft)
        {
            MonitorService.Log.Information("Creating a new draft: {Title}", draft.Title);
            _context.Drafts.Add(draft);
            _context.SaveChanges();
            return CreatedAtAction(nameof(Get), new { id = draft.Id }, draft);
        }

        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] Draft draft)
        {
            if (id != draft.Id)
            {
                MonitorService.Log.Warning("Draft ID mismatch: route {RouteId} vs body {BodyId}", id, draft.Id);
                return BadRequest();
            }

            MonitorService.Log.Information("Updating draft with ID {DraftId}", id);
            _context.Drafts.Update(draft);
            _context.SaveChanges();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            MonitorService.Log.Information("Deleting draft with ID {DraftId}", id);
            var draft = _context.Drafts.Find(id);
            if (draft == null)
            {
                MonitorService.Log.Warning("Draft with ID {DraftId} not found for deletion", id);
                return NotFound();
            }
            _context.Drafts.Remove(draft);
            _context.SaveChanges();
            return NoContent();
        }
    }
}
