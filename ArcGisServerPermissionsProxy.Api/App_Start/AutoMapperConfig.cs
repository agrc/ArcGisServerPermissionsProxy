using ArcGisServerPermissionProxy.Domain;
using ArcGisServerPermissionProxy.Domain.Database;
using ArcGisServerPermissionProxy.Domain.ViewModels;
using AutoMapper;

namespace ArcGisServerPermissionsProxy.Api {

    public class AutoMapperConfig {
        public static void RegisterMaps()
        {
            Mapper.CreateMap<User, UserViewModel>()
                  .ForMember(dest => dest.AccessRules, option => option.MapFrom(src => new UserViewModel.UserAccessRules
                      {
                          EndDate = src.AccessRules.EndDate,
                          StartDate = src.AccessRules.StartDate,
                          Options = src.AccessRules.Options
                      }))
                  .ForMember(dest => dest.Additional, option => option.MapFrom(src => src.Additional))
                  .ForMember(dest => dest.AdminToken, option => option.MapFrom(src => src.AdminToken))
                  .ForMember(dest => dest.Agency, option => option.MapFrom(src => src.Agency))
                  .ForMember(dest => dest.Application, option => option.MapFrom(src => src.Application))
                  .ForMember(dest => dest.Email, option => option.MapFrom(src => src.Email))
                  .ForMember(dest => dest.First, option => option.MapFrom(src => src.First))
                  .ForMember(dest => dest.Last, option => option.MapFrom(src => src.Last))
                  .ForMember(dest => dest.LastLogin, option => option.MapFrom(src => src.LastLogin))
                  .ForMember(dest => dest.Role, option => option.MapFrom(src => src.Role))
                  .ForMember(dest => dest.UserId, option => option.MapFrom(src => src.UserId));

            Mapper.CreateMap<CreateApplicationParams.ApplicationInfo.CustomEmailMarkdown, CustomEmails>()
                  .ForMember(dest => dest.NotifyAdminOfNewUser,
                             option => option.MapFrom(src => src.NotifyAdminOfNewUser))
                  .ForMember(dest => dest.NotifyUserAccepted, option => option.MapFrom(src => src.NotifyUserAccepted));

            Mapper.CreateMap<CreateApplicationParams, Config>()
                  .ForMember(dest => dest.AdminPage, option => option.MapFrom(src => src.Application.AdminPage))
                  .ForMember(dest => dest.AdministrativeEmails, option => option.MapFrom(src => src.AdminEmails))
                  .ForMember(dest => dest.BaseUrl, option => option.MapFrom(src => src.Application.BaseUrl))
                  .ForMember(dest => dest.CustomEmails, option => option.MapFrom(src => src.Application.CustomEmails))
                  .ForMember(dest => dest.Description, option => option.MapFrom(src => src.Application.Description))
                  .ForMember(dest => dest.Roles, option => option.MapFrom(src => src.Roles))
                  .ForMember(dest => dest.UsersCanExpire, option => option.MapFrom(src => src.Application.AccessRules));

#if DEBUG
            Mapper.AssertConfigurationIsValid();
#endif
        }
    }

}