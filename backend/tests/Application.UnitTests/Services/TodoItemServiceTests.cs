using Moq;
using NUnit.Framework;
using Shouldly;
using SchoolClubs.Application.Common.Exceptions;
using SchoolClubs.Application.Services.TodoItems;
using SchoolClubs.Application.Validators;
using SchoolClubs.Domain.Abstractions;
using SchoolClubs.Domain.Entities;

namespace SchoolClubs.Application.UnitTests.Services;

[TestFixture]
[TestOf(typeof(TodoItemService))]
public class TodoItemServiceTests
{
    private Mock<ITodoItemRepository> _repository = null!;
    private TodoItemService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new Mock<ITodoItemRepository>();
        _service = new TodoItemService(_repository.Object,
            new CreateTodoItemRequestValidator(),
            new UpdateTodoItemRequestValidator());
    }

    [Test]
    public async Task CreateAsync_TrimsTitleAndSaves()
    {
        var dto = await _service.CreateAsync(new CreateTodoItemRequest("  Buy milk  "));

        dto.Title.ShouldBe("Buy milk");
        _repository.Verify(r => r.AddAsync(It.Is<TodoItem>(t => t.Title == "Buy milk"), It.IsAny<CancellationToken>()));
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()));
    }

    [Test]
    public async Task CreateAsync_EmptyTitle_ThrowsValidationException()
    {
        var ex = await Should.ThrowAsync<ValidationException>(() => _service.CreateAsync(new CreateTodoItemRequest("")));

        ex.Errors.ShouldContainKey(nameof(CreateTodoItemRequest.Title));
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAsync_Missing_ThrowsNotFound()
    {
        _repository.Setup(r => r.FindByIdAsync(It.IsAny<object[]>())).ReturnsAsync((TodoItem?) null);

        await Should.ThrowAsync<Ardalis.GuardClauses.NotFoundException>(() => _service.GetAsync(Guid.NewGuid()));
    }
}
