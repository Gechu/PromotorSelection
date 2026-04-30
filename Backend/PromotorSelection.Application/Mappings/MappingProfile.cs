using AutoMapper;
using PromotorSelection.Application.Dto;
using PromotorSelection.Application.Students;
using PromotorSelection.Domain.Entities;

namespace PromotorSelection.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Student, StudentDto>();
        CreateMap<CreateStudentCommand, Student>();

    }
}