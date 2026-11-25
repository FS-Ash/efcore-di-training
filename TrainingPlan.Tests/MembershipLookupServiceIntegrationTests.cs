using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TrainingPlan.Services;
using TrainingPlan.Tests.Infrastructure;

namespace TrainingPlan.Tests
{
    public class MembershipLookupServiceIntegrationTests
    {
        [Fact]
        public void MembershipLookupService_ShouldBeScoped()
        {
            using var factory = new SqliteWebApplicationFactory();


            using var scope1 = factory.Services.CreateScope();
            using var scope2 = factory.Services.CreateScope();

            var context1 = scope1.ServiceProvider.GetRequiredService<MembershipLookupService>();
            var context2 = scope1.ServiceProvider.GetRequiredService<MembershipLookupService>();
            var context3 = scope2.ServiceProvider.GetRequiredService<MembershipLookupService>();

            Assert.Same(context1, context2); // Same scope should return the same instance
            Assert.NotSame(context1, context3); // Different scopes should return different instances
        }
    }
}
