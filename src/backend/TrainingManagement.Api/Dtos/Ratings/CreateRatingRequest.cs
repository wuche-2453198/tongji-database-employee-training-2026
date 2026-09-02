namespace TrainingManagement.Api.Dtos.Ratings;
public class CreateRatingRequest
{
    public int CourseId { get; set; }
    public int TrainerId { get; set; }
    public int Score { get; set; }    // 1-5
    public string? Comment { get; set; }
}
