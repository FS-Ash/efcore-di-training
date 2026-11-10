using Microsoft.AspNetCore.Mvc;
using TrainingPlan.Services;
namespace TrainingPlan.Controllers
{
    [ApiController]
    [Route("api/membership-renewal")]
    public class MembershipRenewalController : ControllerBase
    {
        private readonly MembershipLookupCache _lookupCache;

        public MembershipRenewalController(MembershipLookupCache lookupCache)
        {
            _lookupCache = lookupCache;
        }

        [HttpGet("{memberId:int}")]
        public async Task<IActionResult> GetRenewalStatus(int memberId)
        {
            if (memberId <= 0)
            {
                return BadRequest("Member ID must be greater than zero");
            }
            var status = await _lookupCache.GetRenewalStatusAsync(memberId);

            return Ok(new { MemberId = memberId, Status = status });
        }
    }
}
