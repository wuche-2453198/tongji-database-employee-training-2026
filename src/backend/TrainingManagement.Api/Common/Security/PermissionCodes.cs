namespace TrainingManagement.Api.Common.Security;

public static class PermissionCodes
{
    public const string AuthMe = "auth.me";
    public const string RoleRead = "role.read";
    public const string EmployeeRead = "employee.read";
    public const string EmployeeWrite = "employee.write";
    public const string DepartmentRead = "department.read";
    public const string DepartmentWrite = "department.write";
    public const string BlacklistRead = "blacklist.read";
    public const string BlacklistWrite = "blacklist.write";
    public const string CourseRead = "course.read";
    public const string CourseWrite = "course.write";
    public const string RequestCreate = "request.create";
    public const string RequestApprove = "request.approve";
    public const string RequestFile = "request.file";
    public const string RegistrationCreate = "registration.create";
    public const string AttendanceWrite = "attendance.write";
    public const string RatingCreate = "rating.create";
    public const string RatingVerify = "rating.verify";
    public const string TestWrite = "test.write";
    public const string CertificateRead = "certificate.read";
    public const string CertificateWrite = "certificate.write";

    public static IReadOnlyCollection<string> GetDefaultPermissions(string roleCode)
    {
        return roleCode switch
        {
            RoleCodes.Admin => new[]
            {
                AuthMe,
                RoleRead,
                EmployeeRead,
                EmployeeWrite,
                DepartmentRead,
                DepartmentWrite,
                BlacklistRead,
                BlacklistWrite,
                CourseRead,
                CourseWrite,
                RequestApprove,
                RequestFile,
                AttendanceWrite,
                RatingVerify,
                TestWrite,
                CertificateRead,
                CertificateWrite
            },
            RoleCodes.Hr => new[]
            {
                AuthMe,
                EmployeeRead,
                CourseRead,
                RequestFile,
                AttendanceWrite,
                RatingVerify,
                TestWrite,
                CertificateRead,
                CertificateWrite
            },
            RoleCodes.DepartmentManager => new[]
            {
                AuthMe,
                EmployeeRead,
                CourseRead,
                RequestApprove
            },
            _ => new[]
            {
                AuthMe,
                CourseRead,
                RequestCreate,
                RegistrationCreate,
                RatingCreate,
                CertificateRead
            }
        };
    }
}
