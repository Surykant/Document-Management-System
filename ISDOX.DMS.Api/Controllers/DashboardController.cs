using ISDOX.DMS.Application.Dashboard.Queries.GetDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ISDOX.DMS.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DashboardController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue(ClaimTypes.Name)
                              ?? "System";
            bool isAdmin = User.IsInRole("Admin");
            var query = new GetDashboardStatsQuery(currentUser, isAdmin);
            var result = await _mediator.Send(query);

            return Ok(result);
        }
    }
}
