using AutoMapper; 
using MediatR;
using PromotorSelection.Domain.Entities;
using PromotorSelection.Infrastructure.Interfaces;
using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace PromotorSelection.Application.Students;

public record CreateStudentCommand(string NrAlbumu, double SredniaOcen, int UserId) : IRequest<int>;

public class CreateStudentHandler : IRequestHandler<CreateStudentCommand, int>
{
    private readonly IStudentRepository _repository;
    private readonly IMapper _mapper;

    public CreateStudentHandler(IStudentRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<int> Handle(CreateStudentCommand request, CancellationToken cancellationToken)
    {

        var student = _mapper.Map<Student>(request);
        await _repository.AddAsync(student);
        await _repository.SaveChangesAsync();

        return student.Id;
    }
}