using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRMVC.Areas.Identity.Data;
using System.Security.Claims;

namespace SignalRMVC.Controllers
{

    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<BasicChatHub> _basicChatHub;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(
            AppDbContext context,
            UserManager<IdentityUser> userManager,
            IHubContext<BasicChatHub> basicChatHub)
        {
            _db = context;
            _userManager = userManager;
            _basicChatHub = basicChatHub;
        }
        [Authorize]
        public async Task<IActionResult> Index()
        {

            var model = new RoleViewModel();
            var user = await _userManager.GetUserAsync(User);
            if(user is not null)
            {
                var roles1 = await _db.UserRoles
    .Where(ur => ur.UserId == user.Id)
    .ToListAsync();
                var roles = await _userManager.GetRolesAsync(user);
                model.UserRoles = roles;
            }

            return View(model);

        }


        [HttpGet("SendMessageToAll")]
        [Authorize]
        public async Task<IActionResult> SendMessageToAll(string user, string message)
        {
            await _basicChatHub.Clients.All.SendAsync("MessageReceived", user, message);
            return Ok();
        }

        [HttpGet("SendMessageToReceiver")]
        [Authorize]
        public async Task<IActionResult> SendMessageToReceiver(string sender, string receiver, string message)
        {
            var userId = _db.Users.FirstOrDefault(u => u.Email.ToLower() == receiver.ToLower())?.Id;

            if (!string.IsNullOrEmpty(userId))
            {
                await _basicChatHub.Clients.User(userId).SendAsync("MessageReceived", sender, message);
            }
            return Ok();
        }

        [HttpGet("SendMessageToGroup")]
        [Authorize]
        public async Task SendMessageToGroup(string message)
        {
            var user = GetUserId();
            var role = (await GetUserRoles(user)).FirstOrDefault();
            var username = _db.Users.FirstOrDefault(u => u.Id == user)?.Email ?? "";


            if (!string.IsNullOrEmpty(role))
            {
                await _basicChatHub.Clients.Group(role).SendAsync("MessageReceived", username, message);
            }

        }

        private string GetUserId()
        {
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId;
        }

        private async Task<IList<string>> GetUserRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            var roles = await _userManager.GetRolesAsync(user);
            return roles;
        }

    }
}
