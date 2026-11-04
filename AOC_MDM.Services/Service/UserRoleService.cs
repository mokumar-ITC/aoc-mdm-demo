using AutoMapper;
using Microsoft.Extensions.Logging;
using AOC_MDM.Core.Entities;
using AOC_MDM.Core.Interfaces;
using AOC_MDM.Services.DTO;
using AOC_MDM.Services.Interfaces;


namespace AOC_MDM.Services.Service
{

    public class UserRoleService : IUserRoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public string CurrentUserName { get;  set; }
        public UserRoleDTO CurrentUserRole { get;  set; }

        public UserRoleService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<UserRoleDTO> GetUserRoleAsync(string userName)
        {
            CurrentUserName = userName;

            var roles = await _unitOfWork.UserRoleRepository.GetAllAsync();

           // var rolesFilter = roles.Where(x => x.Login.ToLower().Trim() == userName.ToLower().Trim()).FirstOrDefault();
            var rolesFilter = roles.FirstOrDefault(x => x.Login.Equals(userName, StringComparison.OrdinalIgnoreCase));
           
            CurrentUserRole = _mapper.Map<UserRoleDTO>(rolesFilter);

            // return _mapper.Map<UserRoleDTO>(rolesFilter.FirstOrDefault());

            return CurrentUserRole;
        }

        public async Task<bool> HasAccessAsync(string userName, int requiredLevel)
        {
            var userRole = await GetUserRoleAsync(userName);
            return userRole != null && userRole.LevelNo >= requiredLevel;
        }
    }
}

