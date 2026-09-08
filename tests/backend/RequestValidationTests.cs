using System.ComponentModel.DataAnnotations;
using TrainingManagement.Api.Dtos.Course;
using TrainingManagement.Api.Dtos.Trainer;

namespace TrainingManagement.Api.ModuleTests;

internal static class RequestValidationTests
{
    public static IEnumerable<(string Name, Func<Task> Run)> GetTests()
    {
        yield return ("TrainerQuery pagination matches common bounds", () =>
        {
            var query = new TrainerQuery();
            Check(query, "Page", 1, true);
            Check(query, "Page", 0, false);
            Check(query, "PageSize", 100, true);
            Check(query, "PageSize", 101, false);
            return Task.CompletedTask;
        });

        foreach (var request in new object[] { new CreateTrainerRequest(), new UpdateTrainerRequest() })
        {
            yield return ($"{request.GetType().Name} phone matches Oracle length", () =>
            {
                Check(request, "Phone", new string('1', 20), true);
                Check(request, "Phone", new string('1', 21), false);
                return Task.CompletedTask;
            });
        }
        foreach (var request in new object[] { new CreateCourseRequest(), new UpdateCourseRequest() })
        {
            yield return ($"{request.GetType().Name} duration matches Oracle precision", () =>
            {
                Check(request, "DurationHours", 0.1m, true);
                Check(request, "DurationHours", 999.9m, true);
                Check(request, "DurationHours", 0m, false);
                Check(request, "DurationHours", 1000m, false);
                return Task.CompletedTask;
            });
            yield return ($"{request.GetType().Name} capacity matches Oracle precision", () =>
            {
                Check(request, "MaxStudents", 1, true);
                Check(request, "MaxStudents", 999999, true);
                Check(request, "MaxStudents", 0, false);
                Check(request, "MaxStudents", 1000000, false);
                return Task.CompletedTask;
            });
        }
    }

    private static void Check(object request, string member, object value, bool expected)
    {
        var context = new ValidationContext(request) { MemberName = member };
        var valid = Validator.TryValidateProperty(value, context, new List<ValidationResult>());
        TestAssert.Equal(expected, valid, $"{request.GetType().Name}.{member}={value} validation.");
    }
}
