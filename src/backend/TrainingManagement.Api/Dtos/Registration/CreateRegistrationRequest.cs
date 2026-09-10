using System.ComponentModel.DataAnnotations;

namespace TrainingManagement.Api.Dtos.Registration;

public sealed class CreateRegistrationRequest
{
    [Range(1, long.MaxValue, ErrorMessage = "必须选择课程")]
    public long CourseId { get; set; }
}
