using AOC_MDM.Services.DTO;

namespace AOC_MDM.Services.Interfaces
{
    //User Role interface
    public interface IUserRoleService
    {
        string CurrentUserName { get; set; }
        UserRoleDTO CurrentUserRole { get; set; }

        Task<UserRoleDTO> GetUserRoleAsync(string userName);
        Task<bool> HasAccessAsync(string userName, int requiredLevel);
    }
}
