using AutoMapper;
using QualityControl.Application.Features.AuditLogs.Dtos;
using QualityControl.Domain.Entities;

namespace QualityControl.Application.Features.AuditLogs;

public class AuditLogMappingProfile : Profile
{
    public AuditLogMappingProfile()
    {
        CreateMap<AuditLog, AuditLogDto>();
    }
}
