using System.Threading;
using System.Threading.Tasks;
using Ava.Xioa.Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Ava.Xioa.Entities.EfDbContext;

public interface IRepository<TEntity> where TEntity : BaseEntity
{
    EfDbContext DbContext { get; }

    public DbSet<TEntity> DbSet { get; }
    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));

    ValueTask<EntityEntry<TEntity>> AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default(CancellationToken));


    bool DbIsExist { get; }
}