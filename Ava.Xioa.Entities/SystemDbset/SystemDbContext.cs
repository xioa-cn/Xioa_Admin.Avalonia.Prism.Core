﻿using System.Threading.Tasks;
using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Common.Const;
using Ava.Xioa.Common.Models;
using Ava.Xioa.Common.Services;
using Ava.Xioa.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Entities.SystemDbset;

[AutoDbContext(ServiceLifetime.Singleton)]
public class SystemDbContext : EfDbContext.EfDbContext, ISqliteNormalable
{
    protected override string ConnectionString { get; }

    private readonly string _dbFilePath;

    public SystemDbContext(SystemDbConfig systemDbConfig)
    {
        if (systemDbConfig.LiteDbName.EndsWith(".db") || systemDbConfig.LiteDbName.EndsWith(".sqlite3"))
        {
            _dbFilePath = AppDataPath.GetLocalFilePath(systemDbConfig.LiteDbName);
        }
        else
        {
            _dbFilePath = AppDataPath.GetLocalFilePath($"{systemDbConfig.LiteDbName}.db");
        }

        ConnectionString = $"Data Source={_dbFilePath}";
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        UseDbType(optionsBuilder, ConnectionString);
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder, typeof(SystemEntity));
    }

    public async Task DbFileExistOrCreateAsync()
    {
        if (System.IO.File.Exists(_dbFilePath))
        {
            return;
        }

        var createResult = await this.Database.EnsureCreatedAsync();

        if (!createResult)
        {
            throw new DbUpdateException(nameof(SystemDbContext));
        }
    }
}