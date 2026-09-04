namespace TrainingManagement.Api.Dtos.TrainingRequest
{
    public class TrainingRequestQueryDto
    {
        public string? Status { get; set; }     // 按状态筛选
        public int? EmployeeId { get; set; }    // 按员工ID筛选
        public int? CourseId { get; set; }      // 按课程ID筛选
        public int Page { get; set; } = 1;      // 页码（默认第1页）
        public int PageSize { get; set; } = 20; // 每页大小（默认20条）
    }
}
