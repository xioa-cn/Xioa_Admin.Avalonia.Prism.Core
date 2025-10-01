using Ava.Xioa.Common.Attributes;
using Ava.Xioa.Entities.EfDbContext;
using Ava.Xioa.Entities.SystemDbset.SystemThemesInformation.Mapper;
using Microsoft.Extensions.DependencyInjection;

namespace Ava.Xioa.Entities.SystemDbset.SystemThemesInformation;

public interface IUserInformationRepository : IRepository<UserInformation>;

[AutoRepository(typeof(IUserInformationRepository), ServiceLifetime.Singleton)]
public class UserInformationRepository : RepositoryBase<UserInformation>, IUserInformationRepository
{
    public UserInformationRepository(SystemDbContext context) : base(context)
    {
    }
}