using AutoMapper;
using QualityControl.Application.Features.Products.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.Application.Features.Products;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<Product, ProductListItemDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.BatchCount, o => o.MapFrom(s => s.ProductionBatches.Count));

        // Inherits the base mapping, so CategoryName/BatchCount are not restated.
        CreateMap<Product, ProductDto>()
            .IncludeBase<Product, ProductListItemDto>();
    }
}
