namespace TrainingManagement.Api.Entities;

public enum RegistrationCreateOutcome
{
    Created,
    CourseUnavailable,
    CapacityFull,
    Duplicate
}

public sealed record RegistrationCreateResult(
    RegistrationCreateOutcome Outcome,
    long RegId = 0);
