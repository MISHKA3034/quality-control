using AutoMapper;
using QualityControl.Application.Features.Defects.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.Defects;

public class DefectMappingProfile : Profile
{
    public DefectMappingProfile()
    {
        CreateMap<DefectPhoto, DefectPhotoDto>();

        CreateMap<Defect, DefectListItemDto>()
            .ForMember(d => d.InspectionNumber, o => o.MapFrom(s => s.QualityInspection.InspectionNumber))
            .ForMember(d => d.ProductionBatchId, o => o.MapFrom(s => s.QualityInspection.ProductionBatchId))
            .ForMember(d => d.BatchNumber, o => o.MapFrom(s => s.QualityInspection.ProductionBatch.BatchNumber))
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.QualityInspection.ProductionBatch.Product.Name))
            .ForMember(d => d.DepartmentId, o => o.MapFrom(s => s.QualityInspection.ProductionBatch.DepartmentId))
            .ForMember(d => d.DepartmentName, o => o.MapFrom(s => s.QualityInspection.ProductionBatch.Department.Name))
            .ForMember(d => d.DefectTypeName, o => o.MapFrom(s => s.DefectType.Name))
            .ForMember(d => d.PhotoCount, o => o.MapFrom(s => s.Photos.Count))
            .ForMember(d => d.CorrectiveActionCount, o => o.MapFrom(s => s.CorrectiveActions.Count))

            // Restates Defect.IsOpen as an expression, since ProjectTo cannot call
            // the C# property. The two must stay in step.
            .ForMember(d => d.IsOpen, o => o.MapFrom(s =>
                !s.CorrectiveActions.Any()
                || s.CorrectiveActions.Any(a => a.Status != CorrectiveActionStatus.Closed)));

        CreateMap<Defect, DefectDto>()
            .IncludeBase<Defect, DefectListItemDto>()
            .ForMember(d => d.Photos, o => o.MapFrom(s => s.Photos.OrderBy(p => p.UploadedAt)));
    }
}
