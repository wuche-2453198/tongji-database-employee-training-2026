using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;
public sealed class OracleCertificateRepository : ICertificateRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleCertificateRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Registration?> GetRegistrationByIdAsync(int registrationId)
    {
        const string sql = """
            SELECT REG_ID, EMP_ID, COURSE_ID, STATUS
            FROM TRAINING_REGISTRATIONS
            WHERE REG_ID = :RegId
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.QueryFirstOrDefaultAsync<Registration>(sql, new { RegId = registrationId });
    }

    public async Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId)
    {
        const string sql = """
            SELECT COUNT(1) FROM TRAINING_CERTIFICATES
            WHERE EMP_ID = :EmpId AND COURSE_ID = :CourseId
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { EmpId = employeeId, CourseId = courseId });
        return count > 0;
    }

    public async Task<int> CreateAsync(string certificateNo, int employeeId, int courseId, int issuedByEmpId)
    {
        const string sql = """
            INSERT INTO TRAINING_CERTIFICATES
                (CERT_CODE, EMP_ID, COURSE_ID, ISSUE_DATE, ISSUED_BY_EMP_ID, NOTIFIED)
            VALUES
                (:CertCode, :EmpId, :CourseId, TRUNC(SYSDATE), :IssuedByEmpId, 'N')
            RETURNING CERT_ID INTO :CertId
            """;
        var parameters = new DynamicParameters();
        parameters.Add("CertCode", certificateNo);
        parameters.Add("EmpId", employeeId);
        parameters.Add("CourseId", courseId);
        parameters.Add("IssuedByEmpId", issuedByEmpId);
        parameters.Add("CertId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        await connection.ExecuteAsync(sql, parameters);
        return parameters.Get<int>("CertId");
    }

    public async Task<IEnumerable<TrainingCertificate>> GetByEmployeeIdAsync(int employeeId)
    {
        const string sql = """
            SELECT cert.CERT_ID AS CertId, cert.EMP_ID AS EmpId, cert.COURSE_ID AS CourseId,
                   cert.CERT_CODE AS CertCode, cert.ISSUE_DATE AS IssueDate, cert.EXPIRE_DATE AS ExpireDate,
                   cert.NOTIFIED AS Notified, cert.NOTIFIED_AT AS NotifiedAt,
                   cert.ISSUED_BY_EMP_ID AS IssuedByEmpId, cert.CREATED_AT AS CreatedAt,
                   course.COURSE_NAME AS CourseName, emp.EMP_NAME AS EmployeeName
            FROM TRAINING_CERTIFICATES cert
            LEFT JOIN TRAINING_COURSES course ON cert.COURSE_ID = course.COURSE_ID
            LEFT JOIN EMPLOYEES emp ON cert.EMP_ID = emp.EMP_ID
            WHERE cert.EMP_ID = :EmpId
            ORDER BY cert.ISSUE_DATE DESC
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.QueryAsync<TrainingCertificate>(sql, new { EmpId = employeeId });
    }

    public async Task<(IReadOnlyList<TrainingCertificate> Items, int Total)> GetPagedListAsync(
        string? employeeName,
        string? courseName,
        DateTime? startDateFrom,
        DateTime? startDateTo,
        int page,
        int pageSize)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            conditions.Add("emp.EMP_NAME LIKE :EmployeeName");
            parameters.Add("EmployeeName", $"%{employeeName.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(courseName))
        {
            conditions.Add("course.COURSE_NAME LIKE :CourseName");
            parameters.Add("CourseName", $"%{courseName.Trim()}%");
        }

        if (startDateFrom.HasValue)
        {
            conditions.Add("cert.ISSUE_DATE >= :StartDateFrom");
            parameters.Add("StartDateFrom", startDateFrom.Value);
        }

        if (startDateTo.HasValue)
        {
            conditions.Add("cert.ISSUE_DATE <= :StartDateTo");
            parameters.Add("StartDateTo", startDateTo.Value);
        }

        var whereSql = conditions.Count == 0
            ? string.Empty
            : "WHERE " + string.Join(" AND ", conditions);

        var countSql = $"""
            SELECT COUNT(1)
            FROM TRAINING_CERTIFICATES cert
            LEFT JOIN TRAINING_COURSES course ON cert.COURSE_ID = course.COURSE_ID
            LEFT JOIN EMPLOYEES emp ON cert.EMP_ID = emp.EMP_ID
            {whereSql}
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var itemsSql = $"""
            SELECT cert.CERT_ID AS CertId, cert.EMP_ID AS EmpId, cert.COURSE_ID AS CourseId,
                   cert.CERT_CODE AS CertCode, cert.ISSUE_DATE AS IssueDate, cert.EXPIRE_DATE AS ExpireDate,
                   cert.NOTIFIED AS Notified, cert.NOTIFIED_AT AS NotifiedAt,
                   cert.ISSUED_BY_EMP_ID AS IssuedByEmpId, cert.CREATED_AT AS CreatedAt,
                   course.COURSE_NAME AS CourseName, emp.EMP_NAME AS EmployeeName
            FROM TRAINING_CERTIFICATES cert
            LEFT JOIN TRAINING_COURSES course ON cert.COURSE_ID = course.COURSE_ID
            LEFT JOIN EMPLOYEES emp ON cert.EMP_ID = emp.EMP_ID
            {whereSql}
            ORDER BY cert.ISSUE_DATE DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """;

        var items = await connection.QueryAsync<TrainingCertificate>(itemsSql, parameters);
        return (items.ToArray(), total);
    }

    public async Task<(IReadOnlyList<CertificateCandidate> Items, int Total)> GetCandidatesAsync(
        string? employeeName,
        string? courseName,
        int page,
        int pageSize)
    {
        var conditions = new List<string>
        {
            "r.STATUS = 'COMPLETED'",
            """
            NOT EXISTS (
                SELECT 1 FROM TRAINING_CERTIFICATES cert
                WHERE cert.EMP_ID = r.EMP_ID AND cert.COURSE_ID = r.COURSE_ID
            )
            """,
        };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            conditions.Add("e.EMP_NAME LIKE :EmployeeName");
            parameters.Add("EmployeeName", $"%{employeeName.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(courseName))
        {
            conditions.Add("c.COURSE_NAME LIKE :CourseName");
            parameters.Add("CourseName", $"%{courseName.Trim()}%");
        }

        var whereSql = "WHERE " + string.Join(" AND ", conditions);

        var countSql = $"""
            SELECT COUNT(1)
            FROM TRAINING_REGISTRATIONS r
            JOIN EMPLOYEES e ON r.EMP_ID = e.EMP_ID
            JOIN DEPARTMENTS_TRAINING d ON e.DEPT_ID = d.DEPT_ID
            JOIN TRAINING_COURSES c ON r.COURSE_ID = c.COURSE_ID
            {whereSql}
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var total = await connection.ExecuteScalarAsync<int>(countSql, parameters);

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        // 资格判定：课程未配置训后测试则直接合格；否则 POST 成绩需 >= 60。
        var itemsSql = $"""
            SELECT r.REG_ID AS RegId, r.EMP_ID AS EmpId, e.EMP_NAME AS EmployeeName,
                   d.DEPT_NAME AS DepartmentName, r.COURSE_ID AS CourseId, c.COURSE_NAME AS CourseName,
                   r.ACTUAL_HOURS AS ActualHours, r.STATUS AS Status,
                   CASE
                       WHEN c.POST_TEST_URL IS NULL THEN 'Y'
                       WHEN pt.SCORE IS NOT NULL AND pt.SCORE >= 60 THEN 'Y'
                       ELSE 'N'
                   END AS Qualified,
                   CASE
                       WHEN c.POST_TEST_URL IS NOT NULL AND pt.SCORE IS NULL THEN '尚未录入训后测试成绩'
                       WHEN c.POST_TEST_URL IS NOT NULL AND pt.SCORE < 60 THEN '训后测试成绩未达到60分'
                       ELSE NULL
                   END AS QualificationReason
            FROM TRAINING_REGISTRATIONS r
            JOIN EMPLOYEES e ON r.EMP_ID = e.EMP_ID
            JOIN DEPARTMENTS_TRAINING d ON e.DEPT_ID = d.DEPT_ID
            JOIN TRAINING_COURSES c ON r.COURSE_ID = c.COURSE_ID
            LEFT JOIN TRAINING_TESTS pt
                ON pt.EMP_ID = r.EMP_ID AND pt.COURSE_ID = r.COURSE_ID AND pt.TEST_TYPE = 'POST'
            {whereSql}
            ORDER BY r.COMPLETED_AT DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """;

        var items = await connection.QueryAsync<CertificateCandidate>(itemsSql, parameters);
        return (items.ToArray(), total);
    }

    public async Task<TrainingCertificate?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT cert.CERT_ID AS CertId, cert.EMP_ID AS EmpId, cert.COURSE_ID AS CourseId,
                   cert.CERT_CODE AS CertCode, cert.ISSUE_DATE AS IssueDate, cert.EXPIRE_DATE AS ExpireDate,
                   cert.NOTIFIED AS Notified, cert.NOTIFIED_AT AS NotifiedAt,
                   cert.ISSUED_BY_EMP_ID AS IssuedByEmpId, cert.CREATED_AT AS CreatedAt,
                   course.COURSE_NAME AS CourseName, emp.EMP_NAME AS EmployeeName
            FROM TRAINING_CERTIFICATES cert
            LEFT JOIN TRAINING_COURSES course ON cert.COURSE_ID = course.COURSE_ID
            LEFT JOIN EMPLOYEES emp ON cert.EMP_ID = emp.EMP_ID
            WHERE cert.CERT_ID = :CertId
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.QueryFirstOrDefaultAsync<TrainingCertificate>(sql, new { CertId = id });
    }

    public async Task<bool> UpdateNotifyFlagAsync(int id)
    {
        const string sql = """
            UPDATE TRAINING_CERTIFICATES
            SET NOTIFIED = 'Y', NOTIFIED_AT = SYSTIMESTAMP
            WHERE CERT_ID = :CertId
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var rows = await connection.ExecuteAsync(sql, new { CertId = id });
        return rows > 0;
    }

    public async Task<ResultCourseGate> GetCourseGateAsync(int courseId)
    {
        const string sql = """
            SELECT POST_TEST_URL AS "PostTestUrl", START_AT AS "StartAt", END_AT AS "EndAt"
            FROM TRAINING_COURSES
            WHERE COURSE_ID = :CourseId
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var gate = await connection.QueryFirstOrDefaultAsync<ResultCourseGate>(sql, new { CourseId = courseId });
        if (gate is null)
        {
            return new ResultCourseGate { Exists = false };
        }

        gate.Exists = true;
        return gate;
    }

    public async Task<decimal?> GetPostTestScoreAsync(int employeeId, int courseId)
    {
        const string sql = """
            SELECT SCORE FROM TRAINING_TESTS
            WHERE EMP_ID = :EmpId AND COURSE_ID = :CourseId AND TEST_TYPE = 'POST'
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.ExecuteScalarAsync<decimal?>(sql, new { EmpId = employeeId, CourseId = courseId });
    }
}
