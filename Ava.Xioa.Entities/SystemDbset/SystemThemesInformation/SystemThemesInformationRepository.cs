using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Entities.EfDbContext;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Entities.SystemDbset.SystemThemesInformation;

public interface
    ISystemThemesInformationRepository : IRepository<SystemThemesInformation.Mapper.SystemThemesInformation>;

[AutoRepository(typeof(ISystemThemesInformationRepository), ServiceLifetime.Scoped)]
public class SystemThemesInformationRepository : RepositoryBase<SystemThemesInformation.Mapper.SystemThemesInformation>,
    ISystemThemesInformationRepository
{
    public SystemThemesInformationRepository(SystemDbContext dbContext) : base(dbContext)
    {
    }
}