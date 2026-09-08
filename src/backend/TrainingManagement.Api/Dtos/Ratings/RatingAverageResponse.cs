namespace TrainingManagement.Api.Dtos.Ratings;

public class RatingAverageResponse
{
    /// <summary>平均分,无评分时为 null。</summary>
    public decimal? AverageScore { get; set; }

    /// <summary>参与评分的人数。</summary>
    public int RatingCount { get; set; }
}
