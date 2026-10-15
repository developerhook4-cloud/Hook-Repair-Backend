using HookRepair.Modules.Auth.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HookRepair.Modules.Auth.Application.Users
{
    public interface IUserService
    {
        public Task<User> CreateUserAsync(User user, CancellationToken cancellationToken);
        public Task<IEnumerable<User>> GetUsersAsync(CancellationToken cancellationToken);
        public Task<User> UpdateUserAsync(User user, Guid userId, string plainTextPassword, CancellationToken cancellationToken);
        public Task UpdateUserStatusAsync(Guid userId, bool active, CancellationToken cancellationToken);

        public Task<(bool Success, string? ErrorMessage)> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);
    }
}
