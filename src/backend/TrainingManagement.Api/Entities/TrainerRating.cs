namespace TrainingManagement.Api.Entities;
public class TrainerRating
{
    public int RatingId { get; set; }
    public int CourseId { get; set; }
    public int TrainerId { get; set; }
    public int EmpId { get; set; }
    public decimal Score { get; set; }
    public string? RatingComment { get; set; }
    public string HrVerified { get; set; } = "N";
    public int? HrVerifierEmpId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? HrComment { get; set; }
    public DateTime RatedAt { get; set; }
}
