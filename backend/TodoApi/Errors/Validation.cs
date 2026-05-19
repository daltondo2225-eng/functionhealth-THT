using System.ComponentModel.DataAnnotations;

namespace TodoApi.Errors;

public static class Validation
{
    public static void Validate<T>(T model) where T : notnull
    {
        var ctx = new ValidationContext(model);
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(model, ctx, results, validateAllProperties: true)) return;

        var details = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty("").Select(m => new { Field = m, r.ErrorMessage }))
            .GroupBy(x => string.IsNullOrEmpty(x.Field) ? "_" : x.Field)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage ?? "Invalid value.").ToArray());

        var first = results.FirstOrDefault()?.ErrorMessage ?? "Invalid request.";
        throw AppException.Validation(first, details);
    }
}
