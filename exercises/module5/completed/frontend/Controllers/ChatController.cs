using Microsoft.AspNetCore.Mvc;
using GloboTicket.Frontend.Services.AI;

namespace GloboTicket.Frontend.Controllers;

public class ChatController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated is not true &&
            (!Request.Cookies.TryGetValue(ChatHub.AnonymousOwnerCookie, out string owner) ||
             !Guid.TryParseExact(owner, "N", out _)))
        {
            Response.Cookies.Append(
                ChatHub.AnonymousOwnerCookie,
                Guid.NewGuid().ToString("N"),
                new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    Path = "/",
                    SameSite = SameSiteMode.Strict,
                    Secure = Request.IsHttps
                });
        }

        ViewData["Title"] = "Chat Support";
        return View();
    }
}
