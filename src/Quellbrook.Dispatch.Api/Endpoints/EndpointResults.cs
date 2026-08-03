using Quellbrook.Dispatch.Application;

namespace Quellbrook.Dispatch.Api.Endpoints;

internal static class EndpointResults
{
    public static IResult Problem<T>(OperationResult<T> result) => result.Status switch
    {
        OperationStatus.Invalid => Results.Problem(result.Error, statusCode: StatusCodes.Status422UnprocessableEntity),
        OperationStatus.NotFound => Results.Problem(result.Error, statusCode: StatusCodes.Status404NotFound),
        OperationStatus.Conflict => Results.Problem(result.Error, statusCode: StatusCodes.Status409Conflict),
        _ => throw new ArgumentOutOfRangeException(nameof(result), result.Status, "Not a failure."),
    };

    public static IResult Ok<T>(OperationResult<T> result, Func<T, IResult> ok) =>
        result.Status == OperationStatus.Succeeded && result.Value is { } value ? ok(value) : Problem(result);
}
