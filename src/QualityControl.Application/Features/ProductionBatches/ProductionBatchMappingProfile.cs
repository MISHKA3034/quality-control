using AutoMapper;
using QualityControl.Application.Features.ProductionBatches.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.Application.Features.ProductionBatches;

public class ProductionBatchMappingProfile : Profile
{
    public ProductionBatchMappingProfile()
    {
        CreateMap<ProductionBatch, ProductionBatchListItemDto>()
            .ForMember(d => d.ProductName, o => o.MapFrom(s => s.Product.Name))
            .ForMember(d => d.ProductCode, o => o.MapFrom(s => s.Product.Code))
            .ForMember(d => d.DepartmentName, o => o.MapFrom(s => s.Department.Name))
            .ForMember(d => d.ForemanName, o => o.MapFrom(s =>
                s.Foreman.LastName + " " + s.Foreman.FirstName.Substring(0, 1) + "."
                + (s.Foreman.MiddleName == null ? "" : s.Foreman.MiddleName.Substring(0, 1) + ".")))
            .ForMember(d => d.InspectionCount, o => o.MapFrom(s => s.Inspections.Count));

        CreateMap<ProductionBatch, ProductionBatchDto>()
            .IncludeBase<ProductionBatch, ProductionBatchListItemDto>()

            // Computed from the domain after projection — not translatable to SQL.
            .ForMember(d => d.AllowedNextStatuses, o => o.Ignore());
    }
}
