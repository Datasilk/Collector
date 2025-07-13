using Microsoft.AspNetCore.Mvc;

namespace Collector.Controllers
{
    public class HomeController : BaseController
    {
        [Route("/")]
        public IActionResult Index()
        {
            if(Request.HasFormContentType)
            {
                //check for login
                var username = Request.Form["username"];
                var password = Request.Form["password"];
                if (username != "" && password != "")
                {
                    if(username == App.Config.Admin.Username && password == App.Config.Admin.Pass)
                    {
                        HttpContext.Session.SetString("admin", "1");
                        Response.Redirect("Dashboard");
                    }
                }
            }
            return View();
        }
    }
}