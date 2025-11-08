using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Entities.EfDbContext;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Entities.SystemDbset.SystemThemesInformation;

public interface INowUserInformationRepository : IRepository<NowUserInformation>;

[AutoRepository(typeof(INowUserInformationRepository), ServiceLifetime.Scoped)]
public class NowUserInformationRepository
    : RepositoryBase<NowUserInformation>, INowUserInformationRepository
{
    public NowUserInformationRepository(SystemDbContext context)
        : base(context)
    {
    }
}