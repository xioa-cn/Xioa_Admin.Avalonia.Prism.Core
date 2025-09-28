using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Entities.SystemDbset;

[AutoDbContext(ServiceLifetime.Singleton)]
public class SystemDbContext : EfDbContext.EfDbContext
{
    protected override string ConnectionString { get; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder,typeof(SystemEntity));
    }
}