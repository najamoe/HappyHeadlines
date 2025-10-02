using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SubscriberService.Data;
using SubscriberService.Models;

namespace SubscriberService.Controllers
{
    public class SubscribersController : Controller
    {
        private readonly SubscriberServiceContext _context;

        public SubscribersController(SubscriberServiceContext context)
        {
            _context = context;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Subscriber subscriber)
        {
            if (ModelState.IsValid)
            {
                _context.Add(subscriber);
                await _context.SaveChangesAsync();
                return CreatedAtAction(nameof(Get), new { id = subscriber.Id }, subscriber);
            }
            return BadRequest(ModelState);
        }
    }
}
