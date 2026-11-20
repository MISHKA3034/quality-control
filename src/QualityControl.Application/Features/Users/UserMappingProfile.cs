using AutoMapper;
using QualityControl.Application.Features.Users.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.Application.Features.Users;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<Role, RoleDto>();

        CreateMap<User, UserDto>()
            .ForMember(d => d.RoleName, o => o.MapFrom(s => s.Role.Name))
            .ForMember(d => d.RoleDisplayName, o => o.MapFrom(s => s.Role.DisplayName))
            .ForMember(d => d.EmployeeName, o => o.MapFrom(s =>
                s.Employee == null
                    ? null
                    : s.Employee.LastName + " " + s.Employee.FirstName
                      + (s.Employee.MiddleName == null ? "" : " " + s.Employee.MiddleName)))
            .ForMember(d => d.DepartmentName, o => o.MapFrom(s =>
                s.Employee == null ? null : s.Employee.Department.Name))
            .ForMember(d => d.IsLockedOut, o => o.MapFrom(s =>
                s.LockoutEndsAt != null && s.LockoutEndsAt > DateTime.UtcNow));
    }
}
