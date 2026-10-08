using FluentValidation;
using SchoolClubs.Application.Services.TodoItems;

namespace SchoolClubs.Application.Validators;

public class CreateTodoItemRequestValidator : AbstractValidator<CreateTodoItemRequest>
{
    public CreateTodoItemRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
    }
}

public class UpdateTodoItemRequestValidator : AbstractValidator<UpdateTodoItemRequest>
{
    public UpdateTodoItemRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
    }
}
