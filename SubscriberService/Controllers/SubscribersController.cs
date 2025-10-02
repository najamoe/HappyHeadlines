using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubscriberService.Data;
using SubscriberService.Infrastructure;
using SubscriberService.Models;

namespace SubscriberService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscribersController : ControllerBase
    {
        private readonly SubscriberDBContext _context;
        private readonly RabbitMQConnection _rabbitMQConnection;

        public SubscribersController(
            SubscriberDBContext context,
            RabbitMQConnection rabbitMQ)
        {
            _context = context;
            _rabbitMQConnection = rabbitMQ;
        }

        // GET for CreatedAtAction
        [HttpGet("{id}")]
        public async Task<ActionResult<Subscriber>> GetSubscriber(int id)
        {
            var subscriber = await _context.Subscribers.FindAsync(id);
            if (subscriber == null)
            {
                return NotFound();
            }

            return subscriber;
        }

        // POST to create a subscriber
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Subscriber subscriber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try { 

            _context.Subscribers.Add(subscriber);
            await _context.SaveChangesAsync();

            // Publish to RabbitMQ
            await _rabbitMQConnection.PublishSubscriberAsync(subscriber);
                
                return CreatedAtAction(

                nameof(GetSubscriber),           // GET method name
                new { id = subscriber.Id },      // route values for GET
                subscriber                       // returned object
            );

                }
            catch (DbUpdateException)
            {
                
                
                return StatusCode(500, "An error occurred while saving the subscriber.");
            }
            catch (Exception )
            {
               
                return StatusCode(500, "An error occurred while publishing the subscriber.");
            }


            
        }
    }
}
