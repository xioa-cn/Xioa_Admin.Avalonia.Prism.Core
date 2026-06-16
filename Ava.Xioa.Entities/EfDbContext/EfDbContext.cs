using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Ava.Xioa.Common.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyModel;

namespace Ava.Xioa.Entities.EfDbContext;

public abstract class EfDbContext : DbContext
{
    protected abstract string ConnectionString { get; }
    public string DbFilePath { get; set; }

    public bool QueryTracking
    {
        set
        {
            this.ChangeTracker.QueryTrackingBehavior = value
                ? QueryTrackingBehavior.TrackAll
                : QueryTrackingBehavior.NoTracking;
        }
    }

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public EfDbContext() : base()
    {
    }

    /// <summary>
    /// 带配置的构造函数
    /// </summary>
    /// <param name="options">数据库上下文配置选项</param>
    public EfDbContext(DbContextOptions<EfDbContext> options) : base(options)
    {
    }


    protected void UseDbType(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        optionsBuilder.UseSqlite(connectionString);
    }

    protected void OnModelCreating(ModelBuilder modelBuilder, Type type)
    {
        try
        {
            // 获取所有项目引用的程序集
            var businessAssemblies = AssemblyLoadContext.Default.Assemblies.ToList()
                .Where(asm =>
                {
                    string name = asm.GetName().Name ?? string.Empty;
                    return
                        !name.StartsWith("Microsoft.")
                        && !name.StartsWith("System.")
                        && !name.StartsWith("Npgsql.")
                        && !name.StartsWith("Prism.")
                        && !name.StartsWith("SukiUI.")
                        && !name.StartsWith("Pomelo.");
                })
                .ToList();

            foreach (var asm in businessAssemblies)
            {
                Type?[] allTypes;
                try
                {
                    allTypes = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 部分程序集存在无法加载的类型，跳过异常类型
                    allTypes = ex.Types.Where(t => t != null).ToArray();
                }

                if (allTypes.Length == 0) continue;

                // 匹配所有非抽象、继承自基类的实体
                var entityTypes = allTypes
                    .Where(t =>
                        t is not null && t.IsClass
                                      && !t.IsAbstract
                                      && t.IsSubclassOf(type))
                    .ToList();

                foreach (var entityType in entityTypes)
                {
                    modelBuilder.Entity(entityType ?? throw new TypeLoadException());
                }
            }

            base.OnModelCreating(modelBuilder);
        }
        catch (Exception ex)
        {
            var mapPath = ($"Log").MapPath();
            FileHelper.WriteFile(
                mapPath,
                $"sysDBlog_{DateTimeExtensions.SystemNow():yyyyMMddHHmmss}.txt",
                ex.Message + ex.StackTrace + ex.Source
            );
        }
    }
}