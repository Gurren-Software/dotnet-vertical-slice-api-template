using Scalar.AspNetCore;

namespace VerticalSlice.Api.Common.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication MapOpenApiWithScalar(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference();

        return app;
    }
}
