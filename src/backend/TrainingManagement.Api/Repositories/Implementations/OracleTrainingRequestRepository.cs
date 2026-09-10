using System.Data;
using Dapper;
using TrainingManagement.Api.Common.Enums;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class OracleTrainingRequestRepository : ITrainingRequestRepository
{
    private const string SelectColumns = """
        SELECT
            r.REQUEST_ID              AS "Id",
            r.EMP_ID                  AS "EmployeeId",
            e.EMP_NAME                AS "EmployeeName",
            e.DEPT_ID                 AS "DeptId",
            d.DEPT_NAME               AS "DepartmentName",
            r.COURSE_ID               AS "CourseId",
            c.COURSE_NAME             AS "CourseName",
            r.REASON                  AS "RequestReason",
            r.STATUS                  AS "Status",
            r.REQUESTED_AT            AS "CreateTime",
            r.DEPT_APPROVER_EMP_ID    AS "DeptApproverId",
            a.EMP_NAME                AS "DeptApproverName",
            r.DEPT_APPROVED_AT        AS "DeptApproveTime",
            r.DEPT_APPROVE_REMARK     AS "DeptApproveComment",
            r.HR_FILER_EMP_ID         AS "HrApproverId",
            h.EMP_NAME                AS "HrApproverName",
            r.HR_FILED_AT             AS "HrFileTime",
            r.HR_FILE_REMARK          AS "HrFileComment"
        FROM TRAINING_REQUESTS r
            LEFT JOIN EMPLOYEES e ON r.EMP_ID = e.EMP_ID
            LEFT JOIN DEPARTMENTS_TRAINING d ON e.DEPT_ID = d.DEPT_ID
            LEFT JOIN TRAINING_COURSES c ON r.COURSE_ID = c.COURSE_ID
            LEFT JOIN EMPLOYEES a ON r.DEPT_APPROVER_EMP_ID = a.EMP_ID
            LEFT JOIN EMPLOYEES h ON r.HR_FILER_EMP_ID = h.EMP_ID
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public OracleTrainingRequestRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> InsertAsync(TrainingRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            BEGIN
                INSERT INTO TRAINING_REQUESTS (EMP_ID, COURSE_ID, REASON, STATUS)
                VALUES (:EmpId, :CourseId, :Reason, :Status)
                RETURNING REQUEST_ID INTO :NewId;
            END;
            """;

        var parameters = new DynamicParameters();
        parameters.Add("EmpId", request.EmployeeId);
        parameters.Add("CourseId", request.CourseId);
        parameters.Add("Reason", request.RequestReason, DbType.String);
        parameters.Add("Status", TrainingRequestStatusText.Pending);
        parameters.Add("NewId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return parameters.Get<int>("NewId");
    }

    public async Task<TrainingRequest?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var sql = SelectColumns + """

            WHERE r.REQUEST_ID = :Id
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var request = await connection.QueryFirstOrDefaultAsync<TrainingRequest>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));

        return request;
    }

    public async Task<(IReadOnlyList<TrainingRequest> Items, int Total)> SearchAsync(
        string? status,
        int? employeeId,
        int? courseId,
        int? deptId,
        string? employeeName,
        string? courseName,
        string? departmentName,
        DateTime? startDateFrom,
        DateTime? startDateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrEmpty(status))
        {
            conditions.Add("r.STATUS = :Status");
            parameters.Add("Status", status);
        }

        if (employeeId.HasValue)
        {
            conditions.Add("r.EMP_ID = :EmployeeId");
            parameters.Add("EmployeeId", employeeId.Value);
        }

        if (courseId.HasValue)
        {
            conditions.Add("r.COURSE_ID = :CourseId");
            parameters.Add("CourseId", courseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            conditions.Add("e.EMP_NAME LIKE :EmployeeName");
            parameters.Add("EmployeeName", $"%{employeeName}%");
        }

        if (!string.IsNullOrWhiteSpace(courseName))
        {
            conditions.Add("c.COURSE_NAME LIKE :CourseName");
            parameters.Add("CourseName", $"%{courseName}%");
        }

        if (!string.IsNullOrWhiteSpace(departmentName))
        {
            conditions.Add("d.DEPT_NAME LIKE :DepartmentName");
            parameters.Add("DepartmentName", $"%{departmentName}%");
        }

        if (startDateFrom.HasValue)
        {
            conditions.Add("r.REQUESTED_AT >= :StartDateFrom");
            parameters.Add("StartDateFrom", startDateFrom.Value);
        }

        if (startDateTo.HasValue)
        {
            conditions.Add("r.REQUESTED_AT <= :StartDateTo");
            parameters.Add("StartDateTo", startDateTo.Value);
        }

        if (deptId.HasValue)
        {
            conditions.Add("""
                EXISTS (SELECT 1 FROM EMPLOYEES de WHERE de.EMP_ID = r.EMP_ID AND de.DEPT_ID = :DeptId)
                """);
            parameters.Add("DeptId", deptId.Value);
        }

        var whereClause = conditions.Count > 0
            ? "WHERE " + string.Join(" AND ", conditions)
            : string.Empty;

        parameters.Add("Offset", (page - 1) * pageSize);
        parameters.Add("PageSize", pageSize);

        var itemsSql = SelectColumns + $"""

            {whereClause}
            ORDER BY r.REQUESTED_AT DESC
            OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY
            """;

        var countSql = $"""
            SELECT COUNT(*)
            FROM TRAINING_REQUESTS r
            LEFT JOIN EMPLOYEES e ON r.EMP_ID = e.EMP_ID
            LEFT JOIN DEPARTMENTS_TRAINING d ON e.DEPT_ID = d.DEPT_ID
            LEFT JOIN TRAINING_COURSES c ON r.COURSE_ID = c.COURSE_ID
            {whereClause}
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var items = await connection.QueryAsync<TrainingRequest>(
            new CommandDefinition(itemsSql, parameters, cancellationToken: cancellationToken));
        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        return (items.ToArray(), total);
    }

    public async Task<bool> UpdateStatusAsync(
        int id,
        string expectedStatus,
        string newStatus,
        int? approverId,
        string? comment,
        CancellationToken cancellationToken)
    {
        // 审批列与备案列不同：主管动作写 DEPT_*，HR 备案写 HR_*。
        // :Remark 不能写成 :Comment（COMMENT 为 Oracle 保留字，ORA-01745）。
        var sql = newStatus switch
        {
            TrainingRequestStatusText.DeptApproved or TrainingRequestStatusText.DeptRejected => """
                UPDATE TRAINING_REQUESTS
                SET STATUS = :NewStatus,
                    DEPT_APPROVER_EMP_ID = :ApproverId,
                    DEPT_APPROVED_AT = SYSTIMESTAMP,
                    DEPT_APPROVE_REMARK = :Remark,
                    UPDATED_AT = SYSTIMESTAMP
                WHERE REQUEST_ID = :Id AND STATUS = :ExpectedStatus
                """,
            TrainingRequestStatusText.HrFiled => """
                UPDATE TRAINING_REQUESTS
                SET STATUS = :NewStatus,
                    HR_FILER_EMP_ID = :ApproverId,
                    HR_FILED_AT = SYSTIMESTAMP,
                    HR_FILE_REMARK = :Remark,
                    UPDATED_AT = SYSTIMESTAMP
                WHERE REQUEST_ID = :Id AND STATUS = :ExpectedStatus
                """,
            _ => throw new InvalidOperationException($"Unsupported request status transition: {newStatus}"),
        };

        var parameters = new DynamicParameters();
        parameters.Add("NewStatus", newStatus);
        parameters.Add("ExpectedStatus", expectedStatus);
        parameters.Add("ApproverId", approverId, DbType.Int32);
        parameters.Add("Remark", comment, DbType.String);
        parameters.Add("Id", id);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return rows > 0;
    }

    public async Task<bool> ExistsActiveRequestAsync(int employeeId, int courseId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM TRAINING_REQUESTS
            WHERE EMP_ID = :EmployeeId
              AND COURSE_ID = :CourseId
              AND STATUS IN (:Pending, :DeptApproved, :HrFiled)
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new
            {
                EmployeeId = employeeId,
                CourseId = courseId,
                Pending = TrainingRequestStatusText.Pending,
                DeptApproved = TrainingRequestStatusText.DeptApproved,
                HrFiled = TrainingRequestStatusText.HrFiled,
            },
            cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<EmployeeGate> GetEmployeeGateAsync(int employeeId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT STATUS
            FROM EMPLOYEES
            WHERE EMP_ID = :EmployeeId
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var status = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
            sql, new { EmployeeId = employeeId }, cancellationToken: cancellationToken));

        return new EmployeeGate { Exists = status is not null, Status = status };
    }

    public async Task<CourseGate> GetCourseGateAsync(int courseId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COURSE_STATUS AS "Status", START_AT AS "StartAt"
            FROM TRAINING_COURSES
            WHERE COURSE_ID = :CourseId
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var gate = await connection.QueryFirstOrDefaultAsync<CourseGate>(new CommandDefinition(
            sql, new { CourseId = courseId }, cancellationToken: cancellationToken));

        if (gate is null)
        {
            return new CourseGate { Exists = false };
        }

        gate.Exists = true;
        return gate;
    }

    public async Task<int?> GetEmployeeDeptIdAsync(int employeeId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT DEPT_ID
            FROM EMPLOYEES
            WHERE EMP_ID = :EmployeeId
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int?>(new CommandDefinition(
            sql, new { EmployeeId = employeeId }, cancellationToken: cancellationToken));
    }
}
