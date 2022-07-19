using Microsoft.AspNetCore.Mvc;

namespace WebApplication3.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RandomController : ControllerBase
    {
        [HttpGet]
        public int Get()
        {
            return Random
                .Shared
                .Next(1, 100);
        }
    }
}
