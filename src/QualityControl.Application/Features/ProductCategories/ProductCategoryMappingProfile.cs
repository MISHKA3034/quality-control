using AutoMapper;
using QualityControl.Application.Features.ProductCategories.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.Application.Features.ProductCategories;

public class ProductCategoryMappingProfile : Profile
{
    public ProductCategoryMappingProfile()
    {
        CreateMap<ProductCategory, ProductCategoryDto>()
            .ForMember(d => d.ParentCategoryName, o => o.MapFrom(s => s.ParentCategory!.Name))
            .ForMember(d => d.ProductCount, o => o.MapFrom(s => s.Products.Count));
    }
}
