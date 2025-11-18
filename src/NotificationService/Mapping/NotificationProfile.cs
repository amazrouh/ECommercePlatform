using AutoMapper;
using Core.Enums;
using Core.Models;
using NotificationService.DTOs;

namespace NotificationService.Mapping;

/// <summary>
/// AutoMapper profile for notification-related mappings.
/// </summary>
public class NotificationProfile : Profile
{
    public NotificationProfile()
    {
        // Map from request DTO to domain model
        CreateMap<SendNotificationRequest, NotificationMessage>()
            .ConstructUsing((src, ctx) =>
            {
                // For SMS, use provided subject or default to "SMS Notification"
                // For other types, use provided subject or default to "Notification"
                var subject = !string.IsNullOrWhiteSpace(src.Subject)
                    ? src.Subject
                    : (src.Type == NotificationType.Sms ? "SMS Notification" : "Notification");
                return new NotificationMessage(src.To, subject, src.Body, src.Metadata);
            })
            .ForMember(dest => dest.Metadata, opt => opt.Ignore());

        // Map from domain model and type to response DTO
        CreateMap<(NotificationResult Result, Core.Enums.NotificationType Type, string Recipient), NotificationResponse>()
            .ForMember(dest => dest.Success, opt => opt.MapFrom(src => src.Result.Success))
            .ForMember(dest => dest.MessageId, opt => opt.MapFrom(src => src.Result.MessageId))
            .ForMember(dest => dest.Error, opt => opt.MapFrom(src => src.Result.Error))
            .ForMember(dest => dest.SentAt, opt => opt.MapFrom(src => src.Result.SentAt))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.Recipient, opt => opt.MapFrom(src => src.Recipient));
    }
}
