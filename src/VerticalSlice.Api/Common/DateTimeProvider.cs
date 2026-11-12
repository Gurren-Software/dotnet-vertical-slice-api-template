using VerticalSlice.Api.Common;

namespace VerticalSlice.Api.Common;

internal sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
