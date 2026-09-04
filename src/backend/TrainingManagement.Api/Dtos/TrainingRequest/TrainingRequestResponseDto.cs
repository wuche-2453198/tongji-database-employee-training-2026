namespace TrainingManagement.Api.Dtos.TrainingRequest
{
    public class TrainingRequestResponseDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }      // 员工姓名（关联查询）
        public int CourseId { get; set; }
        public string CourseName { get; set; }        // 课程名称（关联查询）
        public string RequestReason { get; set; }
        public string Status { get; set; }
        public string StatusDisplay { get; set; }     // 状态的中文显示
        public int? DeptApproverId { get; set; }
        public string DeptApproverName { get; set; }  // 主管姓名
        public DateTime? DeptApproveTime { get; set; }
        public string? DeptApproveComment { get; set; }
        public int? HrApproverId { get; set; }
        public string HrApproverName { get; set; }    // HR姓名
        public DateTime? HrFileTime { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
