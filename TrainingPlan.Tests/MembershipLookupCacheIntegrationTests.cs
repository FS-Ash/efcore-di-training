using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TrainingPlan.Services;

namespace TrainingPlan.Tests
{
    public class MembershipLookupCacheIntegrationTests
    {
        [Fact]
        public void MembershipLookupCache_ShouldBeScoped()
        {
            using var factory = new WebApplicationFactory<Program>();


            using var scope1 = factory.Services.CreateScope();
            using var scope2 = factory.Services.CreateScope();

            var context1 = scope1.ServiceProvider.GetRequiredService<MembershipLookupCache>();
            var context2 = scope1.ServiceProvider.GetRequiredService<MembershipLookupCache>();
            var context3 = scope2.ServiceProvider.GetRequiredService<MembershipLookupCache>();

            Assert.Same(context1, context2); // Same scope should return the same instance
            Assert.NotSame(context1, context3); // Different scopes should return different instances
        }
    }
}
