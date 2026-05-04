using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PromotorSelection.Application.Dto;
using PromotorSelection.Application.Common.Interfaces;


namespace PromotorSelection.Application.Students;

public record GetStudentsQuery() : IRequest<IEnumerable<StudentDto>>;

public class GetStudentsHandler : IRequestHandler<GetStudentsQuery, IEnumerable<StudentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetStudentsHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<StudentDto>> Handle(GetStudentsQuery request, CancellationToken ct)
    {
        var students = await _context.Students.Include(p => p.User).ToListAsync(ct);

        return _mapper.Map<IEnumerable<StudentDto>>(students);
    }
}