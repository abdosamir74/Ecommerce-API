using Domain.Entities.Identity;
using System.Threading.Tasks;

namespace Application.Common.Interfaces
{
    public interface ITokenService
    {
        Task<string> CreateTokenAsync(AppUser user);
        RefreshToken GenerateRefreshToken();
    }
}