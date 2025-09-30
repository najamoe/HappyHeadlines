using Microsoft.AspNetCore.Mvc;

namespace PublisherService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PublisherController : Controller
    {

        public IActionResult Index()
        {
            return View();
        }
    }
}
