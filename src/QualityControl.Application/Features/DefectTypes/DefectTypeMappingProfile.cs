using AutoMapper;
using QualityControl.Application.Features.DefectTypes.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.Application.Features.DefectTypes;

public class DefectTypeMappingProfile : Profile
{
    public DefectTypeMappingProfile()
    {
        CreateMap<DefectType, DefectTypeDto>()
            .ForMember(d => d.DefectCount, o => o.MapFrom(s => s.Defects.Count));
    }
}
