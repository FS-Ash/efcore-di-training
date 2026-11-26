using Microsoft.EntityFrameworkCore;
using TrainingPlan.Data;
using TrainingPlan.Exceptions;

namespace TrainingPlan.Services
{
    public class MembershipLookupService
    {
        private readonly AppDbContext _db;
        public MembershipLookupService(AppDbContext db)
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

            if (membership is null)
                throw new MembershipNotFoundException(memberId);

            return string.IsNullOrWhiteSpace(membership.Status)
                ? "Unknown"
                : membership.Status;
        }

    }
}
