namespace ChocolateyPackageBuilder.Core.Models;

public sealed class ActionPath
{
    public ActionPathKind Kind { get; set; } = ActionPathKind.Literal;
    public string Value { get; set; } = string.Empty;
}