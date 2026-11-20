using AutoMapper;
using QualityControl.Application.Features.Departments.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.Application.Features.Departments;

public class DepartmentMappingProfile : Profile
{
    public DepartmentMappingProfile()
    {
        CreateMap<Department, DepartmentDto>()
            .ForMember(d => d.ParentDepartmentName, o => o.MapFrom(s => s.ParentDepartment!.Name))

            // Built inline rather than via Employee.FullName: ProjectTo must translate
            // this to SQL, and it cannot call into a C# property getter.
            .ForMember(d => d.ManagerName, o => o.MapFrom(s =>
                s.Manager == null
                    ? null
                    : s.Manager.LastName + " " + s.Manager.FirstName +
                      (s.Manager.MiddleName == null ? "" : " " + s.Manager.MiddleName)))
            .ForMember(d => d.EmployeeCount, o => o.MapFrom(s => s.Employees.Count));
    }
}
