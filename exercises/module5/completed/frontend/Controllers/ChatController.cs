using Microsoft.AspNetCore.Mvc;

namespace GloboTicket.Frontend.Controllers;

public class ChatController(IWebHostEnvironment environment, IConfiguration configuration) : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Chat Support";
        ViewData["McpUiPrototype"] = environment.IsDevelopment() &&
            configuration.GetValue<bool>("McpUiPrototype:Enabled") &&
            Request.Query["mcpUiPrototype"] == "1";
        return View();
    }
}
