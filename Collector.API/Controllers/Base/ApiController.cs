using Microsoft.AspNetCore.Mvc;

namespace Collector.API.Controllers
{
    [Route("api/[controller]")]
    public class ApiController : Controller
    {
        protected Guid GetUserId()
        {
            if (HttpContext.User != null)
            {
                var userId = HttpContext.User.Claims.Where(a => a.Type == "AppUser").FirstOrDefault();
                return userId != null ? Guid.Parse(userId.Value) : Guid.Empty;
            }
            return Guid.Empty;
        }
    }
}
