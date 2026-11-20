using AutoMapper;
using QualityControl.Application.Features.Employees.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.Application.Features.Employees;

public class EmployeeMappingProfile : Profile
{
    public EmployeeMappingProfile()
    {
        // Employee.FullName/ShortName are C# properties, which ProjectTo cannot turn
        // into SQL. The equivalents are rebuilt here from columns so the whole
        // projection stays server-side.
        CreateMap<Employee, EmployeeDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s =>
                s.LastName + " " + s.FirstName + (s.MiddleName == null ? "" : " " + s.MiddleName)))
            .ForMember(d => d.ShortName, o => o.MapFrom(s =>
                s.LastName + " " + s.FirstName.Substring(0, 1) + "."
                + (s.MiddleName == null ? "" : s.MiddleName.Substring(0, 1) + ".")))
            .ForMember(d => d.DepartmentName, o => o.MapFrom(s => s.Department.Name))
            .ForMember(d => d.HasUserAccount, o => o.MapFrom(s => s.User != null));
    }
}
