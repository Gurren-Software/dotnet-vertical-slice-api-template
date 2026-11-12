using System.Text.Json;
using VerticalSlice.Api.Features.Todos;

namespace VerticalSlice.Api.UnitTests.Todos;

// Pins the System.Text.Json behavior the CreateTodo endpoint relies on to normalize DueDate to UTC.
public sealed class CreateTodoRequestDeserializationTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("2026-12-01T10:00:00Z", DateTimeKind.Utc)]
    [InlineData("2026-12-01T10:00:00+02:00", DateTimeKind.Local)]
    [InlineData("2026-12-01T10:00:00", DateTimeKind.Unspecified)]
    public void DueDate_Should_BeDeserializedWithExpectedKind(string dueDate, DateTimeKind expectedKind)
    {
        CreateTodo.Endpoint.Request? request = JsonSerializer.Deserialize<CreateTodo.Endpoint.Request>(
            $$"""{ "dueDate": "{{dueDate}}" }""",
            WebOptions);

        request!.DueDate!.Value.Kind.ShouldBe(expectedKind);
    }

    [Fact]
    public void DueDate_WithOffset_Should_ConvertToTheSameUtcInstant()
    {
        CreateTodo.Endpoint.Request? request = JsonSerializer.Deserialize<CreateTodo.Endpoint.Request>(
            """{ "dueDate": "2026-12-01T10:00:00+02:00" }""",
            WebOptions);

        request!.DueDate!.Value.ToUniversalTime()
            .ShouldBe(new DateTime(2026, 12, 1, 8, 0, 0, DateTimeKind.Utc));
    }
}
