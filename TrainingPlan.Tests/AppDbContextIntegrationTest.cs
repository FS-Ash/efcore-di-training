using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingPlan.Data;

namespace TrainingPlan.Tests
{
    public class DbContextScopeTests
    {
        [Fact]
        public void DbContext_ShouldBeScoped()
        {
            using var factory = new WebApplicationFactory<Program>();

            
            using var scope1 = factory.Services.CreateScope();
            using var scope2 = factory.Services.CreateScope();

            var context1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
            var context2 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
            var context3 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();

            Assert.Same(context1, context2); // Same scope should return the same instance
            Assert.NotSame(context1, context3); // Different scopes should return different instances
        }
    }
}
