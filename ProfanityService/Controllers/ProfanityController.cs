using Microsoft.AspNetCore.Mvc;
using ProfanityService.Models;
using ProfanityService.Data;

namespace ProfanityService.Controllers
{
    [Route("api/profanity")]
    [ApiController]
    public class ProfanityController : ControllerBase
    {
        private readonly ProfanityDbContext _context;
        public ProfanityController(ProfanityDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(_context.Profanities.ToList());
        }
        [HttpPost]
        public IActionResult Create(Profanity profanity)
        {
            _context.Profanities.Add(profanity);
            _context.SaveChanges();
            return CreatedAtAction(nameof(GetAll), profanity);
        }

        [HttpPost("check")]
        public IActionResult Check([FromBody] ProfanityCheckRequest request)
        {
            var profanities = _context.Profanities
                .Select(p => p.Word)
                .ToList();

            bool contains = profanities.Any(p =>
                request.Text.Contains(p, StringComparison.OrdinalIgnoreCase));

            return Ok(new { isClean = !contains });
        }


    }
}