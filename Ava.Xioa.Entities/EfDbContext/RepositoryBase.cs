using System;
using System.Threading;
using System.Threading.Tasks;
using Ava.Xioa.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Ava.Xioa.Entities.EfDbContext;

public abstract class RepositoryBase<TEntity> where TEntity : BaseEntity
{
    public RepositoryBase()
    {
    }

    public RepositoryBase(EfDbContext dbContext)
    {
        this.DefaultDbContext = dbContext ?? throw new Exception("dbContext未实例化。");
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        return EFContext.SaveChangesAsync(cancellationToken);
    }

    public Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        return EFContext.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public ValueTask<EntityEntry<TEntity>> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return this.DbSet.AddAsync(entity, cancellationToken);
    }

    private EfDbContext DefaultDbContext { get; set; }

    private EfDbContext EFContext
    {
        get
        {
            return DefaultDbContext;
        }
    }

    public virtual EfDbContext DbContext
    {
        get { return DefaultDbContext; }
    }

    public DbSet<TEntity> DbSet
    {
        get { return EFContext.Set<TEntity>(); }
    }
}