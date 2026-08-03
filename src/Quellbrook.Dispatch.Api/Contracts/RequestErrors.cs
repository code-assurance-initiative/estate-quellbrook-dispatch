namespace Quellbrook.Dispatch.Api.Contracts;

/// <summary>Collects field errors in the shape of a validation problem response.</summary>
internal sealed class RequestErrors
{
    private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            messages = [];
            _errors[field] = messages;
        }

        messages.Add(message);
    }

    public void Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Add(field, "is required");
        }
    }

    public void Present<T>(T? value, string field)
        where T : struct
    {
        if (value is null)
        {
            Add(field, "is required");
        }
    }

    public Dictionary<string, string[]> ToDictionary() =>
        _errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
}
