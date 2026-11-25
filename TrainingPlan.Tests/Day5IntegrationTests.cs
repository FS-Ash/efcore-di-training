
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TrainingPlan.Data;
using TrainingPlan.Models;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using TrainingPlan.Tests.Infrastructure;

namespace TrainingPlan.Tests;

public class Day5IntegrationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IntegratedApproval_ShouldHandleCommitAndRollback(
        bool shouldFail)
    {
        using var factory = new SqliteWebApplicationFactory(shouldFail);

        int orderId;
        // Create database and seed an order.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            var createOrder = new Order
            {
                Status = "Pending"

            };

            db.Orders.Add(createOrder);
            await db.SaveChangesAsync();

            orderId = createOrder.OrderId;
        }

        using var client = factory.CreateClient();

        const string correlationId = "integration-test-001";
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);

        var response = await client.PostAsync(
            $"/api/orders/{orderId}/approve",
            null);

        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var correlationIds));
        Assert.Equal(correlationId, Assert.Single(correlationIds!));

        // Verify database state using a fresh DbContext.
        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await verifyDb.Orders.SingleAsync();
        var auditCount = await verifyDb.OrderAuditLogs.CountAsync();

        if (shouldFail)
        {
            Assert.Equal(
                HttpStatusCode.InternalServerError,
                response.StatusCode);

            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);

            var problem = await response.Content
                .ReadFromJsonAsync<ProblemDetails>();

            Assert.NotNull(problem);
            Assert.Equal(500, problem.Status);
            Assert.Equal("Internal server error", problem.Title);
            Assert.Equal("An unexpected error occurred.", problem.Detail);

            Assert.Equal("Pending", order.Status);
            Assert.Equal(0, auditCount);
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Approved", order.Status);
            Assert.Equal(1, auditCount);
        }
    }

    [Fact]
    public async Task ApproveOrder_WhenOrderNotFound_ShouldReturnProblemDetails()
    {
        using var factory = new SqliteWebApplicationFactory();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            await db.Database.EnsureCreatedAsync();
        }

        using var client = factory.CreateClient();

        const int orderId = 999999;

        var response = await client.PostAsync(
            $"/api/orders/{orderId}/approve",
            null);

        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var correlationIds));
        Assert.False(string.IsNullOrWhiteSpace(Assert.Single(correlationIds!)));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Order not found", problem.Title);
        Assert.Equal(
            $"Order with ID {orderId} not found.",
            problem.Detail);

        Assert.Equal(
            $"/api/orders/{orderId}/approve",
            problem.Instance);
    }

    [Fact]
    public async Task ApproveOrder_WhenInvalidStatus_ShouldReturnConflict()
    {
        using var factory = new SqliteWebApplicationFactory();

        int orderId;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            await db.Database.EnsureCreatedAsync();

            var order = new Order
            {
                Status = "Cancelled"
            };

            db.Orders.Add(order);
            await db.SaveChangesAsync();

            orderId = order.OrderId;
        }

        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            $"/api/orders/{orderId}/approve",
            null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content
            .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Invalid order state", problem.Title);
        Assert.Equal(
            $"Order with ID {orderId} cannot be approved from status 'Cancelled'.",
            problem.Detail);

        using var verifyScope = factory.Services.CreateScope();

        var verifyDb = verifyScope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var savedOrder = await verifyDb.Orders
            .SingleAsync(o => o.OrderId == orderId);

        var auditCount = await verifyDb.OrderAuditLogs
            .CountAsync(a => a.OrderId == orderId);

        Assert.Equal("Cancelled", savedOrder.Status);
        Assert.Equal(0, auditCount);
    }

}



