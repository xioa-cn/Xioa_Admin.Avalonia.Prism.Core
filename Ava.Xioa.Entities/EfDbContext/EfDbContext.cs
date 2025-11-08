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
            var compilationLibrary = DependencyContext
                .Default?
                .CompileLibraries
                .Where(x => !x.Serviceable && x.Type != "package" && x.Type == "project");

            if (compilationLibrary != null)
                foreach (var compilation in compilationLibrary)
                {
                    // 通过反射加载指定类型的实体
                    // 查找所有继承自指定类型的类，并注册到EF Core中
                    AssemblyLoadContext.Default
                        .LoadFromAssemblyName(new AssemblyName(compilation.Name))
                        .GetTypes().Where(x => x.GetTypeInfo().BaseType != null
                                               && x.BaseType == (type)).ToList()
                        .ForEach(t => { modelBuilder.Entity(t); });
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