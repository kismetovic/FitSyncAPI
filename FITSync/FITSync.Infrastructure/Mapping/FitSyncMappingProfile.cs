using AutoMapper;
using FITSync.Contracts.AdditionalServices;
using FITSync.Contracts.Dashboard;
using FITSync.Contracts.Notifications;
using FITSync.Contracts.Payments;
using FITSync.Contracts.Reservations;
using FITSync.Contracts.Reviews;
using FITSync.Contracts.Trainings;
using FITSync.Contracts.TrainingTypes;
using FITSync.Contracts.Users;
using FITSync.Domain.Entities;
using FITSync.Domain.Models;

namespace FITSync.Infrastructure.Mapping
{
    public class FitSyncMappingProfile : Profile
    {
        public FitSyncMappingProfile()
        {
            CreateMap<Training, TrainingResponse>();
            CreateMap<TrainingType, TrainingTypeSummaryResponse>();
            CreateMap<TrainingInsertRequest, Training>();
            CreateMap<TrainingUpdateRequest, Training>();

            CreateMap<TrainingType, TrainingTypeResponse>();
            CreateMap<TrainingTypeInsertRequest, TrainingType>();
            CreateMap<TrainingTypeUpdateRequest, TrainingType>();

            CreateMap<Reservation, ReservationResponse>()
                .ForMember(d => d.AdditionalServiceIds, o => o.MapFrom(s => s.ReservationServices.Select(rs => rs.AdditionalServiceId).ToList()));
            CreateMap<ReservationInsertRequest, Reservation>()
                .ForMember(d => d.ReservationServices, o => o.Ignore());
            CreateMap<ReservationUpdateRequest, Reservation>()
                .ForMember(d => d.ReservationServices, o => o.Ignore());
            CreateMap<User, UserSummaryResponse>();
            CreateMap<Training, TrainingSummaryResponse>();

            CreateMap<Review, ReviewResponse>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User != null ? s.User.UserName : null))
                .ForMember(d => d.TrainingName, o => o.MapFrom(s => s.Training != null ? s.Training.Name : null));
            CreateMap<ReviewInsertRequest, Review>();
            CreateMap<ReviewUpdateRequest, Review>();

            CreateMap<Payment, PaymentResponse>()
                .ForMember(d => d.UserName, o => o.MapFrom(s =>
                    s.Reservation != null && s.Reservation.User != null
                        ? (s.Reservation.User.Name ?? s.Reservation.User.UserName)
                        : null))
                .ForMember(d => d.UserEmail, o => o.MapFrom(s =>
                    s.Reservation != null && s.Reservation.User != null
                        ? s.Reservation.User.Email
                        : null))
                .ForMember(d => d.TrainingName, o => o.MapFrom(s =>
                    s.Reservation != null && s.Reservation.Training != null
                        ? s.Reservation.Training.Name
                        : null));
            CreateMap<PaymentInsertRequest, Payment>();
            CreateMap<PaymentUpdateRequest, Payment>();

            CreateMap<Notification, NotificationResponse>();
            CreateMap<NotificationInsertRequest, Notification>();
            CreateMap<NotificationUpdateRequest, Notification>();

            CreateMap<User, UserResponse>()
                .ForMember(d => d.Roles, o => o.MapFrom(s => s.Roles != null
                    ? s.Roles.Where(ur => ur.Role != null).Select(ur => ur.Role!.Name!).ToList()
                    : new List<string>()));
            CreateMap<UserInsertRequest, User>()
                .ForMember(d => d.PasswordHash, o => o.Ignore())
                .ForMember(d => d.Roles, o => o.Ignore());
            CreateMap<UserUpdateRequest, User>()
                .ForMember(d => d.PasswordHash, o => o.Ignore())
                .ForMember(d => d.Roles, o => o.Ignore());

            CreateMap<AdditionalService, AdditionalServiceResponse>();
            CreateMap<AdditionalServiceInsertRequest, AdditionalService>();
            CreateMap<AdditionalServiceUpdateRequest, AdditionalService>();
        }
    }
}

