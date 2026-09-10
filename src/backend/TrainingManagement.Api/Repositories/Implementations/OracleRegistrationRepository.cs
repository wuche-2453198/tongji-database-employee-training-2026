using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using TrainingManagement.Api.Dtos.Registration;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class OracleRegistrationRepository : IRegistrationRepository
{
    private const string SelectColumns = """
        r.REG_ID AS "RegId",
        r.REQUEST_ID AS "RequestId",
        r.EMP_ID AS "EmpId",
        e.EMP_NAME AS "EmpName",
        e.DEPT_ID AS "DeptId",
        d.DEPT_NAME AS "DeptName",
        r.COURSE_ID AS "CourseId",
        c.COURSE_NAME AS "CourseName",
        c.COURSE_TYPE AS "CourseType",
        c.DURATION_HOURS AS "DurationHours",
        t.TRAINER_NAME AS "TrainerName",
        c.START_AT AS "StartAt",
        c.END_AT AS "EndAt",
        c.LOCATION AS "Location",
        c.COURSE_STATUS AS "CourseStatus",
        c.MAX_STUDENTS AS "MaxStudents",
        r.STATUS AS "Status",
        r.REGISTERED_AT AS "RegisteredAt",
        r.COMPLETED_AT AS "CompletedAt",
        r.ACTUAL_HOURS AS "ActualHours",
        r.CANCELED_AT AS "CanceledAt",
        r.CANCEL_REASON AS "CancelReason",
        r.UPDATED_AT AS "UpdatedAt",
        a.ATTEND_ID AS "AttendId",
        a.SIGNIN_TYPE AS "SigninType",
        a.SIGNED_IN_AT AS "SignedInAt",
        a.LATENESS_MINUTES AS "LatenessMinutes",
        a.DEDUCT_HOURS AS "DeductHours",
        a.REMARK AS "AttendanceRemark"
        """;

    private const string FromClause = """
        FROM TRAINING_REGISTRATIONS r
        JOIN EMPLOYEES e
            ON r.EMP_ID = e.EMP_ID
        JOIN DEPARTMENTS_TRAINING d
            ON e.DEPT_ID = d.DEPT_ID
        JOIN TRAINING_COURSES c
            ON r.COURSE_ID = c.COURSE_ID
        LEFT JOIN TRAINERS t
            ON c.TRAINER_ID = t.TRAINER_ID
        LEFT JOIN TRAINING_ATTENDANCE a
            ON r.REG_ID = a.REG_ID
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public OracleRegistrationRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<(IReadOnlyList<RegistrationRecord> Items, long Total)> GetAllAsync(
        RegistrationQuery query,
        long? empId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return (Array.Empty<RegistrationRecord>(), 0);
        }

        var (whereSql, parameters) = BuildConditions(
            query,
            empId);

        var countSql = $"""
            SELECT COUNT(1)
            {FromClause}
            {whereSql}
            """;

        var orderBy = MapSort(query.SortBy ?? "startTime");
        var direction = string.Equals(
            query.SortDirection,
            "asc",
            StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : "DESC";

        var querySql = $"""
            SELECT
                {SelectColumns}
            {FromClause}
            {whereSql}
            ORDER BY {orderBy} {direction}
            OFFSET :Offset ROWS
            FETCH NEXT :PageSize ROWS ONLY
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        var total = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                countSql,
                parameters,
                cancellationToken: cancellationToken));

        parameters.Add(
            "Offset",
            (query.Page - 1) * query.PageSize);

        parameters.Add(
            "PageSize",
            query.PageSize);

        var items = await connection.QueryAsync<RegistrationRecord>(
            new CommandDefinition(
                querySql,
                parameters,
                cancellationToken: cancellationToken));

        return (items.ToArray(), total);
    }

    public async Task<RegistrationRecord?> GetByIdAsync(
        long regId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = $"""
            SELECT
                {SelectColumns}
            {FromClause}
            WHERE r.REG_ID = :RegId
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<RegistrationRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    RegId = regId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<RegistrationContextRecord?> GetContextAsync(
        long regId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = """
            SELECT
                r.REG_ID AS "RegId",
                r.REQUEST_ID AS "RequestId",
                r.EMP_ID AS "EmpId",
                r.COURSE_ID AS "CourseId",
                r.STATUS AS "Status",
                r.ACTUAL_HOURS AS "ActualHours",
                c.COURSE_STATUS AS "CourseStatus",
                c.START_AT AS "StartAt",
                c.END_AT AS "EndAt",
                c.DURATION_HOURS AS "DurationHours",
                c.MAX_STUDENTS AS "MaxStudents"
            FROM TRAINING_REGISTRATIONS r
            JOIN TRAINING_COURSES c
                ON r.COURSE_ID = c.COURSE_ID
            WHERE r.REG_ID = :RegId
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<RegistrationContextRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    RegId = regId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<long?> FindFiledRequestIdAsync(
        long empId,
        long courseId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = """
            SELECT REQUEST_ID
            FROM TRAINING_REQUESTS
            WHERE EMP_ID = :EmpId
              AND COURSE_ID = :CourseId
              AND STATUS = 'HR_FILED'
            FETCH FIRST 1 ROWS ONLY
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        return await connection.ExecuteScalarAsync<long?>(
            new CommandDefinition(
                sql,
                new
                {
                    EmpId = empId,
                    CourseId = courseId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<EmployeeEligibilityRecord?> GetEmployeeEligibilityAsync(
        long empId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = """
            SELECT
                e.EMP_ID AS "EmpId",
                e.STATUS AS "Status",
                (
                    SELECT COUNT(1)
                    FROM BLACKLIST b
                    WHERE b.EMP_ID = e.EMP_ID
                      AND b.STATUS = 'ACTIVE'
                      AND (b.END_AT IS NULL OR b.END_AT > SYSTIMESTAMP)
                ) AS "ActiveBlacklistCount"
            FROM EMPLOYEES e
            WHERE e.EMP_ID = :EmpId
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<EmployeeEligibilityRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    EmpId = empId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<CourseEligibilityRecord?> GetCourseEligibilityAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = """
            SELECT
                COURSE_ID AS "CourseId",
                COURSE_STATUS AS "CourseStatus",
                START_AT AS "StartAt",
                END_AT AS "EndAt",
                DURATION_HOURS AS "DurationHours",
                MAX_STUDENTS AS "MaxStudents"
            FROM TRAINING_COURSES
            WHERE COURSE_ID = :CourseId
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<CourseEligibilityRecord>(
            new CommandDefinition(
                sql,
                new
                {
                    CourseId = courseId
                },
                cancellationToken: cancellationToken));
    }

    public async Task<RegistrationCreateResult> CreateAsync(
        RegistrationRecord registration,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return new RegistrationCreateResult(
                RegistrationCreateOutcome.CourseUnavailable);
        }

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(
                cancellationToken);

        try
        {
            const string lockSql = """
                SELECT
                    COURSE_STATUS AS "CourseStatus",
                    START_AT AS "StartAt",
                    MAX_STUDENTS AS "MaxStudents"
                FROM TRAINING_COURSES
                WHERE COURSE_ID = :CourseId
                FOR UPDATE
                """;

            var course = await connection.QuerySingleOrDefaultAsync<CourseLockRecord>(
                new CommandDefinition(
                    lockSql,
                    new
                    {
                        CourseId = registration.CourseId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (course is null
                || !string.Equals(
                    course.CourseStatus,
                    "PUBLISHED",
                    StringComparison.OrdinalIgnoreCase)
                || course.StartAt is null
                || course.StartAt <= DateTime.Now)
            {
                await transaction.RollbackAsync(cancellationToken);

                return new RegistrationCreateResult(
                    RegistrationCreateOutcome.CourseUnavailable);
            }

            var activeCount = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(
                    """
                    SELECT COUNT(1)
                    FROM TRAINING_REGISTRATIONS
                    WHERE COURSE_ID = :CourseId
                      AND STATUS <> 'CANCELED'
                    """,
                    new
                    {
                        CourseId = registration.CourseId
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (activeCount >= course.MaxStudents)
            {
                await transaction.RollbackAsync(cancellationToken);

                return new RegistrationCreateResult(
                    RegistrationCreateOutcome.CapacityFull);
            }

            var parameters = new DynamicParameters();

            parameters.Add(
                "RequestId",
                registration.RequestId);

            parameters.Add(
                "EmpId",
                registration.EmpId);

            parameters.Add(
                "CourseId",
                registration.CourseId);

            parameters.Add(
                "NewRegId",
                dbType: DbType.Int64,
                direction: ParameterDirection.Output);

            try
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO TRAINING_REGISTRATIONS (
                            REQUEST_ID,
                            EMP_ID,
                            COURSE_ID,
                            STATUS,
                            REGISTERED_AT,
                            UPDATED_AT
                        )
                        VALUES (
                            :RequestId,
                            :EmpId,
                            :CourseId,
                            'REGISTERED',
                            SYSTIMESTAMP,
                            SYSTIMESTAMP
                        )
                        RETURNING REG_ID INTO :NewRegId
                        """,
                        parameters,
                        transaction: transaction,
                        cancellationToken: cancellationToken));
            }
            catch (OracleException exception)
                when (exception.Number == 1)
            {
                await transaction.RollbackAsync(cancellationToken);

                return new RegistrationCreateResult(
                    RegistrationCreateOutcome.Duplicate);
            }

            await transaction.CommitAsync(cancellationToken);

            return new RegistrationCreateResult(
                RegistrationCreateOutcome.Created,
                parameters.Get<long>("NewRegId"));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<bool> CancelAsync(
        long regId,
        string? cancelReason,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = """
            UPDATE TRAINING_REGISTRATIONS
            SET
                STATUS = 'CANCELED',
                CANCELED_AT = SYSTIMESTAMP,
                CANCEL_REASON = :CancelReason,
                UPDATED_AT = SYSTIMESTAMP
            WHERE REG_ID = :RegId
              AND STATUS = 'REGISTERED'
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    RegId = regId,
                    CancelReason = cancelReason
                },
                cancellationToken: cancellationToken));

        return affectedRows > 0;
    }

    public async Task<bool> MarkAbsentAsync(
        long regId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = """
            UPDATE TRAINING_REGISTRATIONS
            SET
                STATUS = 'ABSENT',
                ACTUAL_HOURS = 0,
                UPDATED_AT = SYSTIMESTAMP
            WHERE REG_ID = :RegId
              AND STATUS = 'REGISTERED'
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    RegId = regId
                },
                cancellationToken: cancellationToken));

        return affectedRows > 0;
    }

    public async Task<bool> CompleteAsync(
        long regId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = """
            UPDATE TRAINING_REGISTRATIONS
            SET
                STATUS = 'COMPLETED',
                COMPLETED_AT = SYSTIMESTAMP,
                UPDATED_AT = SYSTIMESTAMP
            WHERE REG_ID = :RegId
              AND STATUS = 'SIGNED_IN'
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    RegId = regId
                },
                cancellationToken: cancellationToken));

        return affectedRows > 0;
    }

    public async Task<RegistrationSummaryRecord> GetSummaryAsync(
        long? courseId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return new RegistrationSummaryRecord();
        }

        const string countSql = """
            SELECT
                NVL(COUNT(1), 0) AS "Total",
                NVL(SUM(CASE WHEN STATUS = 'REGISTERED' THEN 1 ELSE 0 END), 0) AS "Registered",
                NVL(SUM(CASE WHEN STATUS = 'SIGNED_IN' THEN 1 ELSE 0 END), 0) AS "SignedIn",
                NVL(SUM(CASE WHEN STATUS = 'ABSENT' THEN 1 ELSE 0 END), 0) AS "Absent",
                NVL(SUM(CASE WHEN STATUS = 'COMPLETED' THEN 1 ELSE 0 END), 0) AS "Completed",
                NVL(SUM(CASE WHEN STATUS = 'CANCELED' THEN 1 ELSE 0 END), 0) AS "Canceled"
            FROM TRAINING_REGISTRATIONS
            WHERE (:CourseId IS NULL OR COURSE_ID = :CourseId)
            """;

        const string maxStudentsSql = """
            SELECT MAX_STUDENTS
            FROM TRAINING_COURSES
            WHERE COURSE_ID = :CourseId
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        var counts = await connection.QuerySingleAsync<RegistrationCountSummary>(
            new CommandDefinition(
                countSql,
                new
                {
                    CourseId = courseId
                },
                cancellationToken: cancellationToken));

        int? maxStudents = null;
        if (courseId.HasValue)
        {
            maxStudents = await connection.ExecuteScalarAsync<int?>(
                new CommandDefinition(
                    maxStudentsSql,
                    new
                    {
                        CourseId = courseId.Value
                    },
                    cancellationToken: cancellationToken));
        }

        return new RegistrationSummaryRecord
        {
            Total = counts.Total,
            Registered = counts.Registered,
            SignedIn = counts.SignedIn,
            Absent = counts.Absent,
            Completed = counts.Completed,
            Canceled = counts.Canceled,
            MaxStudents = maxStudents
        };
    }

    private sealed class RegistrationCountSummary
    {
        public long Total { get; set; }
        public long Registered { get; set; }
        public long SignedIn { get; set; }
        public long Absent { get; set; }
        public long Completed { get; set; }
        public long Canceled { get; set; }
    }

    private static (string WhereSql, DynamicParameters Parameters) BuildConditions(
        RegistrationQuery query,
        long? empId)
    {
        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (empId.HasValue)
        {
            conditions.Add("r.EMP_ID = :EmpId");
            parameters.Add(
                "EmpId",
                empId.Value);
        }
        else if (query.EmpId.HasValue)
        {
            conditions.Add("r.EMP_ID = :EmpId");
            parameters.Add(
                "EmpId",
                query.EmpId.Value);
        }

        if (query.CourseId.HasValue)
        {
            conditions.Add("r.COURSE_ID = :CourseId");
            parameters.Add(
                "CourseId",
                query.CourseId.Value);
        }

        if (query.DeptId.HasValue)
        {
            conditions.Add("e.DEPT_ID = :DeptId");
            parameters.Add(
                "DeptId",
                query.DeptId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.EmpName))
        {
            conditions.Add("e.EMP_NAME LIKE :EmpName");
            parameters.Add(
                "EmpName",
                $"%{query.EmpName.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(query.CourseName))
        {
            conditions.Add("c.COURSE_NAME LIKE :CourseName");
            parameters.Add(
                "CourseName",
                $"%{query.CourseName.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            conditions.Add("r.STATUS = :Status");
            parameters.Add(
                "Status",
                query.Status.Trim().ToUpperInvariant());
        }

        if (query.SignedIn.HasValue)
        {
            conditions.Add(query.SignedIn.Value
                ? "a.ATTEND_ID IS NOT NULL"
                : "a.ATTEND_ID IS NULL");
        }

        if (query.StartAtFrom.HasValue)
        {
            conditions.Add("c.START_AT >= :StartAtFrom");
            parameters.Add(
                "StartAtFrom",
                query.StartAtFrom.Value);
        }

        if (query.StartAtTo.HasValue)
        {
            conditions.Add("c.START_AT <= :StartAtTo");
            parameters.Add(
                "StartAtTo",
                query.StartAtTo.Value);
        }

        var whereSql = conditions.Count == 0
            ? string.Empty
            : "WHERE " + string.Join(" AND ", conditions);

        return (whereSql, parameters);
    }

    private static string MapSort(string sortBy)
    {
        return sortBy.ToLowerInvariant() switch
        {
            "regdate" => "r.REGISTERED_AT",
            "completedat" => "r.COMPLETED_AT",
            _ => "c.START_AT"
        };
    }

    private sealed class CourseLockRecord
    {
        public string CourseStatus { get; init; } = string.Empty;

        public DateTime? StartAt { get; init; }

        public int MaxStudents { get; init; }
    }
}
