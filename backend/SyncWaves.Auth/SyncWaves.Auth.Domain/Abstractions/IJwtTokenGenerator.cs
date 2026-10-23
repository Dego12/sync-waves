using SyncWaves.Auth.Domain.Entities;

namespace SyncWaves.Auth.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateToken(UserEntity user, out DateTime expiresAt);
}
