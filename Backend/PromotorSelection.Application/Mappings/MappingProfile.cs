using AutoMapper;
using PromotorSelection.Application.Dto;
using PromotorSelection.Application.Students;
using PromotorSelection.Domain.Entities;

namespace PromotorSelection.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Student, StudentDto>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email));

        CreateMap<Promotor, PromotorDto>()
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.User.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.User.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.Topics, opt => opt.MapFrom(src => src.Topics));
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.AlbumNumber, opt => opt.MapFrom(src => src.Student != null ? src.Student.AlbumNumber : null))
            .ForMember(dest => dest.GradeAverage, opt => opt.MapFrom(src => src.Student != null ? src.Student.GradeAverage : null))
            .ForMember(dest => dest.StudentLimit, opt => opt.MapFrom(src => src.Promotor != null ? src.Promotor.StudentLimit : (int?)null));
        CreateMap<Topic, TopicDto>();
    }
}