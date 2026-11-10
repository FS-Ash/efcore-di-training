using Microsoft.EntityFrameworkCore;
using TrainingPlan.Data;

namespace TrainingPlan.Services
{
    public class MembershipLookupCache
    {
        private readonly AppDbContext _db;
        public MembershipLookupCache(AppDbContext db)
        {
            _db = db;
        }

        public async Task<string> GetRenewalStatusAsync(int memberId)
        {
            if (memberId <= 0)
                throw new ArgumentOutOfRangeException(nameof(memberId));

            var membership = await _db.Memberships
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MemberId == memberId);
            var status = membership?.Status ?? "Unknown";

            return string.IsNullOrWhiteSpace(status) ? "Unknown" : status;
        }       

       
    }
}
