using AutoMapper;
using QualityControl.Application.Features.CorrectiveActions.Dtos;
using QualityControl.Domain.Entities;
using QualityControl.Domain.Enums;

namespace QualityControl.Application.Features.CorrectiveActions;

public class CorrectiveActionMappingProfile : Profile
{
    public CorrectiveActionMappingProfile()
    {
        CreateMap<CorrectiveActionHistory, CorrectiveActionHistoryDto>()
            .ForMember(d => d.ChangedByUserName, o => o.MapFrom(s => s.ChangedByUser.UserName));

        CreateMap<CorrectiveAction, CorrectiveActionListItemDto>()
            .ForMember(d => d.DefectDescription, o => o.MapFrom(s => s.Defect.Description))
            .ForMember(d => d.DefectSeverity, o => o.MapFrom(s => s.Defect.Severity))
            .ForMember(d => d.DefectTypeName, o => o.MapFrom(s => s.Defect.DefectType.Name))
            .ForMember(d => d.ProductionBatchId, o => o.MapFrom(s => s.Defect.QualityInspection.ProductionBatchId))
            .ForMember(d => d.BatchNumber,
                o => o.MapFrom(s => s.Defect.QualityInspection.ProductionBatch.BatchNumber))
            .ForMember(d => d.ProductName,
                o => o.MapFrom(s => s.Defect.QualityInspection.ProductionBatch.Product.Name))
            .ForMember(d => d.DepartmentId,
                o => o.MapFrom(s => s.Defect.QualityInspection.ProductionBatch.DepartmentId))
            .ForMember(d => d.DepartmentName,
                o => o.MapFrom(s => s.Defect.QualityInspection.ProductionBatch.Department.Name))
            .ForMember(d => d.AssignedToName, o => o.MapFrom(s =>
                s.AssignedTo == null
                    ? null
                    : s.AssignedTo.LastName + " " + s.AssignedTo.FirstName.Substring(0, 1) + "."
                      + (s.AssignedTo.MiddleName == null ? "" : s.AssignedTo.MiddleName.Substring(0, 1) + ".")))

            // Mirrors CorrectiveAction.IsOverdue as an expression ProjectTo can turn
            // into SQL. DateTime.UtcNow is evaluated by the database.
            .ForMember(d => d.IsOverdue, o => o.MapFrom(s =>
                s.DueDate != null
                && s.Status != CorrectiveActionStatus.Closed
                && s.DueDate < DateTime.UtcNow.Date));

        CreateMap<CorrectiveAction, CorrectiveActionDto>()
            .IncludeBase<CorrectiveAction, CorrectiveActionListItemDto>()
            .ForMember(d => d.VerifiedByName, o => o.MapFrom(s =>
                s.VerifiedByEmployee == null
                    ? null
                    : s.VerifiedByEmployee.LastName + " " + s.VerifiedByEmployee.FirstName))
            .ForMember(d => d.History, o => o.MapFrom(s => s.History.OrderByDescending(h => h.ChangedAt)))

            // Filled from the domain's transition table after projection.
            .ForMember(d => d.AllowedNextStatuses, o => o.Ignore());
    }
}
