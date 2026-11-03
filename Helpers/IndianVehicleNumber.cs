using System.Text.RegularExpressions;

namespace RiceMillProject.Helpers;

public static partial class IndianVehicleNumber
{
    public const string ValidationMessage = "Enter a valid Indian vehicle number, e.g. UP63AT3088, DL1CAF5032 or 22BH1234AA.";

    public static string Normalize(string? value) =>
        Regex.Replace((value ?? string.Empty).Trim().ToUpperInvariant(), @"[\s-]+", string.Empty);

    public static bool IsValid(string? value) => VehicleNumberPattern().IsMatch(Normalize(value));

    [GeneratedRegex(@"^(?:[A-Z]{2}[0-9]{1,2}[A-Z]{1,3}[0-9]{1,4}|[0-9]{2}BH[0-9]{4}[A-Z]{1,2})$")]
    private static partial Regex VehicleNumberPattern();
}
