namespace TrainingManagement.Api.Dtos.TrainingRequest
{
    public class CreateTrainingRequestDto
    {
        public int CourseId { get; set; }        // 申请的课程ID
        public string RequestReason { get; set; } // 申请理由
    }
}
