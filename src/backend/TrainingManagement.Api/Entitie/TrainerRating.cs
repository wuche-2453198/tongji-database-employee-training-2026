namespace TrainingManagement.Api.Entities;
public class TrainerRating
{
    public int RatingId { get; set; }
    public int EmployeeId { get; set; }
    public int CourseId { get; set; }
    public int TrainerId { get; set; }
    public int Score { get; set; }          // 1-5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? VerifyStatus { get; set; } = "PENDING";
    public string? VerifyComment { get; set; }
    public DateTime? VerifiedAt { get; set; }
}
