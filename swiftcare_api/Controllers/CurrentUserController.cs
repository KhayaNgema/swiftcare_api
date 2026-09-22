using swiftcare_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace swiftcare_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CurrentUserController : ControllerBase
    {
        private readonly UserManager<UserBaseModel> _userManager;

        public CurrentUserController(UserManager<UserBaseModel> userManager)
        {
            _userManager = userManager;
        }

        [Authorize(AuthenticationSchemes = "Bearer")]
        [HttpGet("current_user")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Unauthorized();

            var roles = await _userManager.GetRolesAsync(user);

            var response = new Dictionary<string, object>
            {
                ["Id"] = user.Id,
                ["FirstName"] = user.FirstName,
                ["LastName"] = user.LastName,
                ["Email"] = user.Email,
                ["PhoneNumber"] = user.PhoneNumber,
                ["Roles"] = roles,
                ["ProfilePicture"] = user.ProfilePicture
            };

            return Ok(response);
        }
    }
}