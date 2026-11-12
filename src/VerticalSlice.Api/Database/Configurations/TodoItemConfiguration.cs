using VerticalSlice.Api.Features.Todos;
using VerticalSlice.Api.Features.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace VerticalSlice.Api.Database.Configurations;

internal sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.HasKey(t => t.Id);

        builder.HasOne<User>().WithMany().HasForeignKey(t => t.UserId);
    }
}
