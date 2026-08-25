using AutoMapper;
using FITSync.Contracts.AdditionalServices;
using FITSync.Contracts.Faqs;
using FITSync.Contracts.Support;
using FITSync.Contracts.Memberships;
using FITSync.Contracts.Notifications;
using FITSync.Contracts.Payments;
using FITSync.Contracts.Reservations;
using FITSync.Contracts.Reviews;
using FITSync.Contracts.Trainers;
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
            // --- Trainings ---
            CreateMap<Training, TrainingResponse>()
                .ForMember(d => d.TrainerName, o => o.MapFrom(s => s.Trainer != null ? s.Trainer.FirstName + " " + s.Trainer.LastName : null));
            CreateMap<TrainingType, TrainingTypeSummaryResponse>();
            CreateMap<TrainingInsertRequest, Training>();
            CreateMap<TrainingUpdateRequest, Training>();

            CreateMap<TrainingType, TrainingTypeResponse>();

            CreateMap<Faq, FaqResponse>();
            CreateMap<FaqInsertRequest, Faq>();
            CreateMap<FaqUpdateRequest, Faq>();

            CreateMap<SupportContact, SupportContactResponse>();
            CreateMap<TrainingTypeInsertRequest, TrainingType>();
            CreateMap<TrainingTypeUpdateRequest, TrainingType>();

            // --- Trainers ---
            CreateMap<Trainer, TrainerResponse>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName));
            CreateMap<TrainerAvailability, TrainerAvailabilityResponse>();
            CreateMap<TrainerInsertRequest, Trainer>();
            CreateMap<TrainerUpdateRequest, Trainer>();

            // --- Reservations ---
            // Status, UserId and the audit fields are never mapped from a request: they are
            // set by the service through the state machine.
            CreateMap<Reservation, ReservationResponse>()
                .ForMember(d => d.AdditionalServiceIds,
                    o => o.MapFrom(s => s.ReservationServices.Select(rs => rs.AdditionalServiceId).ToList()))
                .ForMember(d => d.AllowedNextStatuses, o => o.Ignore())
                .ForMember(d => d.IsPaid, o => o.Ignore())
                .ForMember(d => d.StatusHistory, o => o.MapFrom(s => s.StatusHistory));

            CreateMap<ReservationStatusHistory, ReservationStatusHistoryResponse>();

            CreateMap<ReservationInsertRequest, Reservation>()
                .ForMember(d => d.ReservationServices, o => o.Ignore())
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.Ignore())
                .ForMember(d => d.TotalPrice, o => o.Ignore())
                .ForMember(d => d.UserMembershipId, o => o.Ignore());

            CreateMap<ReservationUpdateRequest, Reservation>()
                .ForMember(d => d.ReservationServices, o => o.Ignore())
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.Ignore())
                .ForMember(d => d.TotalPrice, o => o.Ignore());

            CreateMap<User, UserSummaryResponse>();
            CreateMap<Training, TrainingSummaryResponse>()
                .ForMember(d => d.TrainerName, o => o.MapFrom(s => s.Trainer != null ? s.Trainer.FirstName + " " + s.Trainer.LastName : null));

            // --- Reviews ---
            CreateMap<Review, ReviewResponse>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User != null ? s.User.UserName : null))
                .ForMember(d => d.TrainingName, o => o.MapFrom(s => s.Training != null ? s.Training.Name : null));

            // --- Payments ---
            CreateMap<Payment, PaymentResponse>()
                // Owner of a package payment comes from the membership, since there is no
                // reservation to read it from.
                .ForMember(d => d.UserName, o => o.MapFrom(s =>
                    s.Reservation != null && s.Reservation.User != null
                        ? (s.Reservation.User.Name ?? s.Reservation.User.UserName)
                        : s.UserMembership != null && s.UserMembership.User != null
                            ? (s.UserMembership.User.Name ?? s.UserMembership.User.UserName)
                            : null))
                .ForMember(d => d.UserEmail, o => o.MapFrom(s =>
                    s.Reservation != null && s.Reservation.User != null
                        ? s.Reservation.User.Email
                        : s.UserMembership != null && s.UserMembership.User != null
                            ? s.UserMembership.User.Email
                            : null))
                .ForMember(d => d.TrainingName, o => o.MapFrom(s =>
                    s.Reservation != null && s.Reservation.Training != null
                        ? s.Reservation.Training.Name
                        : null))
                // A payment for a package has no reservation behind it, so the owner and
                // the subject have to come from the membership instead. Without this the
                // client sees a payment with no name against it.
                .ForMember(d => d.MembershipPackageName, o => o.MapFrom(s =>
                    s.UserMembership != null && s.UserMembership.MembershipPackage != null
                        ? s.UserMembership.MembershipPackage.Name
                        : null));

            CreateMap<PaymentInsertRequest, Payment>()
                .ForMember(d => d.ProviderOrderId, o => o.Ignore())
                .ForMember(d => d.CapturedAt, o => o.Ignore())
                .ForMember(d => d.ConfirmedByUserId, o => o.Ignore());
            CreateMap<PaymentUpdateRequest, Payment>()
                .ForMember(d => d.ReservationId, o => o.Ignore())
                .ForMember(d => d.ProviderOrderId, o => o.Ignore());

            // --- Notifications ---
            CreateMap<Notification, NotificationResponse>();
            CreateMap<NotificationInsertRequest, Notification>()
                .ForMember(d => d.IsRead, o => o.Ignore());
            CreateMap<NotificationUpdateRequest, Notification>()
                .ForMember(d => d.UserId, o => o.Ignore());

            // --- Users ---
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

            // --- Additional services ---
            CreateMap<AdditionalService, AdditionalServiceResponse>();
            CreateMap<AdditionalServiceInsertRequest, AdditionalService>();
            CreateMap<AdditionalServiceUpdateRequest, AdditionalService>();

            // --- Memberships ---
            CreateMap<MembershipPackage, MembershipPackageResponse>()
                .ForMember(d => d.TrainingTypeName, o => o.MapFrom(s => s.TrainingType != null ? s.TrainingType.Name : null));
            CreateMap<MembershipPackageInsertRequest, MembershipPackage>();
            CreateMap<MembershipPackageUpdateRequest, MembershipPackage>();

            CreateMap<UserMembership, UserMembershipResponse>()
                .ForMember(d => d.SessionsRemaining, o => o.Ignore())
                .ForMember(d => d.IsUsable, o => o.Ignore())
                .ForMember(d => d.MembershipPackageName, o => o.Ignore())
                .ForMember(d => d.TrainingTypeId, o => o.Ignore());
        }
    }
}
