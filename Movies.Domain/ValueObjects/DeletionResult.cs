namespace Movies.Domain.ValueObjects;

public class DeletionResult
{
    public string Result { get; init; } = string.Empty;
    public bool IsSuccess => Result == "ok";
    public string? Error { get; init; }
}
