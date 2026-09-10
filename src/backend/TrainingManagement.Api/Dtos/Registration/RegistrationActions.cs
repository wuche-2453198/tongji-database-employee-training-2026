namespace TrainingManagement.Api.Dtos.Registration;

public sealed class RegistrationActions
{
    public ActionEligibility Cancel { get; set; } = new();

    public ActionEligibility SignIn { get; set; } = new();

    public ActionEligibility Complete { get; set; } = new();

    public ActionEligibility MarkAbsent { get; set; } = new();
}
