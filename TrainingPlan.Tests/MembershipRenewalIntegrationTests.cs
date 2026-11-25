using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TrainingPlan.Data;
using TrainingPlan.Tests.Infrastructure;

namespace TrainingPlan.Tests;

public class MembershipRenewalIntegrationTests
{
    [Fact]
    public async Task GetRenewalStatus_WhenMembershipNotFound_ShouldReturnProblemDetails()
    {
        using var factory = new SqliteWebApplicationFactory();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            await db.Database.EnsureCreatedAsync();
        }

        using var client = factory.CreateClient();

        const int memberId = 999999;

        var response = await client.GetAsync(
            $"/api/membership-renewal/{memberId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Membership not found", problem.Title);
        Assert.Equal(
            $"Membership for member ID {memberId} not found.",
            problem.Detail);
    }
}