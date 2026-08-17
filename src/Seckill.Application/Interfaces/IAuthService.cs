using Seckill.Domain.Entities;

namespace Seckill.Application.Interfaces;

public interface IAuthService
{
    Task<User> RegisterAsync(string email, string password);
    Task<string> LoginAsync(string email, string password); // 回傳 JWT
}