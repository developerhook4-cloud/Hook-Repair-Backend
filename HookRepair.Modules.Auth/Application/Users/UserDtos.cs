using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HookRepair.Modules.Auth.Application.Users
{
    public sealed class SaveUserRequest
    {
        [Required, StringLength(300)]
        public string Name { get; set; } = string.Empty;
        [Required, StringLength(300), EmailAddress]
        public string Email { get; set; } = string.Empty;
        [StringLength(300)]
        public string Password { get; set; } = string.Empty;
        [Required]
        public Guid RoleId {  get; set; }
        [Required]
        public bool Active { get; set; }
    }

    public sealed record UpdateProfileRequest(
        [Required] string Name,
        [Required, EmailAddress] string Email,
        string? OldPassword,
        string? NewPassword
    );

    public sealed record UserResponse(Guid id, string name, string email, string roleName, Guid roleId, bool active);
}
