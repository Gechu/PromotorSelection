using AutoMapper;
using MediatR;
using PromotorSelection.Application.Dto;
using PromotorSelection.Infrastructure.Interfaces;

namespace PromotorSelection.Application.Students;

public record GetStudentsQuery() : IRequest<IEnumerable<StudentDto>>;

public class GetStudentsHandler : IRequestHandler<GetStudentsQuery, IEnumerable<StudentDto>>
{
    private readonly IStudentRepository _repository;
    private readonly IMapper _mapper;

    public GetStudentsHandler(IStudentRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<StudentDto>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        var students = await _repository.GetAllAsync();

        return _mapper.Map<IEnumerable<StudentDto>>(students);
    }
}