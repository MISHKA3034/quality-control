using AutoMapper;
using QualityControl.Application.Features.QualityInspections.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.Application.Features.QualityInspections;

public class QualityInspectionMappingProfile : Profile
{
    public QualityInspectionMappingProfile()
    {
        CreateMap<InspectionResult, InspectionResultDto>();

        CreateMap<QualityInspection, QualityInspectionListItemDto>()
            .ForMember(d => d.BatchNumber, o => o.MapFrom(s => s.ProductionBatch.BatchNumber))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.ProductionBatch.Product.Name))
            .ForMember(d => d.DepartmentId, o => o.MapFrom(s => s.ProductionBatch.DepartmentId))
            .ForMember(d => d.DepartmentName, o => o.MapFrom(s => s.ProductionBatch.Department.Name))
            .ForMember(d => d.InspectorName, o => o.MapFrom(s =>
                s.Inspector.LastName + " " + s.Inspector.FirstName.Substring(0, 1) + "."
                + (s.Inspector.MiddleName == null ? "" : s.Inspector.MiddleName.Substring(0, 1) + ".")))
            .ForMember(d => d.DefectCount, o => o.MapFrom(s => s.Defects.Count));

        CreateMap<QualityInspection, QualityInspectionDto>()
            .IncludeBase<QualityInspection, QualityInspectionListItemDto>()
            .ForMember(d => d.Results, o => o.MapFrom(s => s.Results.OrderBy(r => r.SortOrder)));
    }
}
