using System;

namespace TrainingManagement.Api.Entities
{
    public class TrainingRequest
    {
        public int Id { get; set; }
        public int EmployeeId { set; get; }
        public string EmployeeName { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public string RequestReason { get; set; }
        public string Status { get; set; }
        public int? DeptApproverId { get; set; }
        public string DeptApproverName { get; set; }
        public DateTime? DeptApproveTime { get; set; }
        public string DeptApproveComment { get; set; }
        public int? HrApproverId { get; set; }
        public string HrApproverName { get; set; }
        public DateTime? HrFileTime { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
