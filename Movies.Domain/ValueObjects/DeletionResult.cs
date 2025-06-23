namespace Movies.Domain.ValueObjects;

public class DeletionResult
{
    public string Result { get; set; } = string.Empty;
    public bool IsSuccess => Result == "ok";
    public string? Error { get; set; }
}
