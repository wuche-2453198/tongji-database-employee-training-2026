using System;

namespace TrainingManagement.Api.Entities
{
    public class TrainingRequest
    {
        public int Id { get; set; }
        public int EmployeeId { set; get; }
        public int CourseId { get; set; }
        public string RequestReason { get; set; }
        public string Status { get; set; }
        public int? DeptApproverId { get; set; }
        public DateTime? DeptApproveTime { get; set; }
        public string DeptApproveComment { get; set; }
        public int? HrApproverId { get; set; }
        public DateTime? HrFileTime { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
