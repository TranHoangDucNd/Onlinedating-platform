﻿using AutoMapper;
using WebDating.DTOs;
using WebDating.DTOs.Post;
using WebDating.Entities.MessageEntities;
using WebDating.Entities.PostEntities;
using WebDating.Entities.ProfileEntities;
using WebDating.Entities.UserEntities;
using WebDating.Extensions;
using WebDating.Utilities;

namespace WebDating.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<AppUser, MemberDto>()
                .ForMember(d => d.PhotoUrl,
                    opt => opt.MapFrom(src => src.Photos.FirstOrDefault(x => x.IsMain).Url))
                .ForMember(d => d.Age,
                    opt => opt.MapFrom(src => src.DateOfBirth.CaculateAge()))
                .ForMember(d => d.DatingProfile,
                    opt => opt.MapFrom(src => src.DatingProfile))
                .ReverseMap();


            CreateMap<Photo, PhotoDto>();
            CreateMap<RegisterDto, AppUser>();
            CreateMap<MemberUpdateDto, AppUser>();

            CreateMap<Message, MessageDto>() //gửi tin
           .ForMember(d => d.SenderPhotoUrl, o => o.MapFrom(s =>
                 s.Sender.Photos.FirstOrDefault(x => x.IsMain).Url))
           .ForMember(d => d.RecipientPhotoUrl, o => o.MapFrom(s =>
                 s.Recipient.Photos.FirstOrDefault(x => x.IsMain).Url));

            CreateMap<DateTime, DateTime>().ConvertUsing(d => DateTime.SpecifyKind(d, DateTimeKind.Utc));
            CreateMap<DateTime?, DateTime?>().ConvertUsing(d => d.HasValue ?
                DateTime.SpecifyKind(d.Value, DateTimeKind.Utc) : null);

            CreateMap<UserInterest, UserInterestVM>().ReverseMap();
            CreateMap<DatingProfile, DatingProfileVM>().ReverseMap();

            CreateMap<DatingProfile, DatingProfileDto>()
                .ForMember(dest => dest.WhereToDate,
                    opt => opt.MapFrom(s => s.WhereToDate.GetDisplayName()))
                .ForMember(dest => dest.WhereToDateCode,
                    opt => opt.MapFrom(s => s.WhereToDate))
                .ForMember(dest => dest.HeightFrom,
                    opt => opt.MapFrom(s => s.HeightFrom))
                .ForMember(dest => dest.HeightTo,
                    opt => opt.MapFrom(s => s.HeightTo))
                .ForMember(dest => dest.WeightFrom,
                    opt => opt.MapFrom(s => s.WeightFrom))
                .ForMember(dest => dest.WeightTo,
                    opt => opt.MapFrom(s => s.WeightTo))
                .ForMember(dest => dest.DatingObject,
                    opt => opt.MapFrom(s => s.DatingObject.GetDisplayName()))
                .ForMember(dest => dest.DatingObjectCode,
                    opt => opt.MapFrom(s => s.DatingObject))
                .ForMember(dest => dest.DatingAgeFrom, opt => opt.MapFrom(s => s.DatingAgeFrom))
                .ForMember(dest => dest.DatingAgeTo, opt => opt.MapFrom(s => s.DatingAgeTo))

                .ForMember(dest => dest.UserInterests,
                    opt => opt.MapFrom(s => s.UserInterests.Select(ui => new UserInterestDto
                    {
                        Id = ui.Id,
                        DatingProfileId = ui.DatingProfileId,
                        InterestName = ui.InterestName.GetDisplayName(),
                        InterestNameCode = ui.InterestName,
                        InterestType = ui.InterestType
                    }).ToList()))
                .ForMember(dest => dest.Occupations,
                    opt => opt.MapFrom(s => s.Occupations.Select(ui => new OccupationDto
                    {
                        Id = ui.Id,
                        DatingProfileId = ui.DatingProfileId,
                        OccupationName = ui.OccupationName.GetDisplayName(),
                        OccupationNameCode = ui.OccupationName,
                        OccupationType = ui.OccupationType
                    }).ToList()));

            CreateMap<Post, PostResponseDto>()
                .ForMember(dest => dest.LikeNumber, o => o.MapFrom(m => m.ReactionLogs.Count()))
                .ForMember(dest => dest.CommentNumber, o => o.MapFrom(m => m.Comments.Count()))
                .ForMember(dest => dest.Images, o => o.MapFrom(s => s.Images.Select(x => x.Path).ToList()))
                .ForPath(dest => dest.UserShort.Id, o => o.MapFrom(s => s.User.Id))
                .ForPath(dest => dest.UserShort.FullName, o => o.MapFrom(s => s.User.UserName))
                .ForPath(dest => dest.UserShort.KnownAs, o => o.MapFrom(s => s.User.KnownAs))
                .ForPath(dest => dest.UserShort.Image, o => o.MapFrom(s => s.User.Photos.FirstOrDefault(x => x.IsMain).Url))
                .ReverseMap();

            CreateMap<Post, ShowPostAdminDto>()
                .ForMember(dest => dest.Images, o => o.MapFrom(s => s.Images.Select(x => x.Path).ToList()))
                .ForPath(dest => dest.UserShort.Id, o => o.MapFrom(s => s.UserId))
                .ForPath(dest => dest.UserShort.FullName, o => o.MapFrom(s => s.User.UserName))
                .ForPath(dest => dest.UserShort.KnownAs, o => o.MapFrom(s => s.User.KnownAs))
                .ForPath(dest => dest.UserShort.Image, o => o.MapFrom(s => s.User.Photos.FirstOrDefault(x => x.IsMain).Url));

            CreateMap<PostReportDto, PostReportDetail>()
                .ReverseMap();

            CreateMap<PostReportDetail, PostReportAdminDto>()
              .ForMember(dest => dest.Report, opt => opt.MapFrom(x => x.Report.GetDisplayName()))
              .ForMember(dest => dest.KnownAs, o => o.MapFrom(s => s.User.KnownAs));

            CreateMap<AppUser, MembersLockDto>()
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(s => s.Photos.FirstOrDefault(x => x.IsMain).Url));

        }
    }
}
