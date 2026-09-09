namespace TrainingManagement.Api.Entities;

public enum SignInOutcome
{
    Success,
    RegistrationConflict,
    DuplicateAttendance
}

public sealed record SignInResult(
    SignInOutcome Outcome,
    long AttendId = 0);
