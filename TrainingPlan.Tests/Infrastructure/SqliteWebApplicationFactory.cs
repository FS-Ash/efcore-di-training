using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TrainingPlan.Data;

namespace TrainingPlan.Tests.Infrastructure;

public class SqliteWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;
    private readonly bool _shouldFail;

    public SqliteWebApplicationFactory(bool shouldFail = false)
    {
        _shouldFail = shouldFail;

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<
                IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);

                if (_shouldFail)
                {
                    options.AddInterceptors(
                        new FailBeforeTransactionCommit());
                }
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            base.Dispose(disposing);
        }
        finally
        {
            if (disposing)
            {
                _connection.Dispose();
            }
        }
    }
}