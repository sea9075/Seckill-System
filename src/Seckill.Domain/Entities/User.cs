namespace Seckill.Domain.Entities;

public enum UserRole
{
    Member = 0,
    Admin = 1
}

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public UserRole Role { get; set; } = UserRole.Member;
    public DateTime CreatedAt { get; set; }
}