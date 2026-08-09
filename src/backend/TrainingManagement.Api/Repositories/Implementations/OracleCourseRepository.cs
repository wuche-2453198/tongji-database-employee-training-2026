using System.Data;
using Dapper;
using TrainingManagement.Api.Dtos.Course;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class OracleCourseRepository : ICourseRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleCourseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<(IReadOnlyList<TrainingCourse> Items, long Total)> GetAllAsync(
        CourseQuery query,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return (Array.Empty<TrainingCourse>(), 0);
        }

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.CourseName))
        {
            conditions.Add("c.COURSE_NAME LIKE :CourseName");
            parameters.Add("CourseName", $"%{query.CourseName}%");
        }

        if (!string.IsNullOrWhiteSpace(query.CourseType))
        {
            conditions.Add("c.COURSE_TYPE = :CourseType");
            parameters.Add("CourseType", query.CourseType);
        }

        if (!string.IsNullOrWhiteSpace(query.CourseStatus))
        {
            conditions.Add("c.COURSE_STATUS = :CourseStatus");
            parameters.Add("CourseStatus", query.CourseStatus);
        }

        if (query.StartAtFrom.HasValue)
        {
            conditions.Add("c.START_AT >= :StartAtFrom");
            parameters.Add("StartAtFrom", query.StartAtFrom.Value);
        }

        if (query.StartAtTo.HasValue)
        {
            conditions.Add("c.START_AT <= :StartAtTo");
            parameters.Add("StartAtTo", query.StartAtTo.Value);
        }

        var whereSql = conditions.Count == 0
            ? string.Empty
            : "WHERE " + string.Join(" AND ", conditions);

        var countSql = $"""
            SELECT COUNT(1)
            FROM TRAINING_COURSES c
            {whereSql}
            """;

        var querySql = $"""
            SELECT
                c.COURSE_ID AS "CourseId",
                c.COURSE_NAME AS "CourseName",
                c.COURSE_TYPE AS "CourseType",
                c.DURATION_HOURS AS "DurationHours",
                c.TRAINER_ID AS "TrainerId",
                t.TRAINER_NAME AS "TrainerName",
                c.MAX_STUDENTS AS "MaxStudents",
                c.START_AT AS "StartAt",
                c.END_AT AS "EndAt",
                c.LOCATION AS "Location",
                c.COURSE_STATUS AS "CourseStatus",
                c.BUDGET_AMOUNT AS "BudgetAmount",
                c.DEPT_ID AS "DeptId",
                d.DEPT_NAME AS "DeptName",
                c.PRE_TEST_URL AS "PreTestUrl",
                c.POST_TEST_URL AS "PostTestUrl",
                c.MATERIAL_URL AS "MaterialUrl",
                c.CREATED_AT AS "CreatedAt",
                c.UPDATED_AT AS "UpdatedAt"
            FROM TRAINING_COURSES c
            LEFT JOIN TRAINERS t
                ON c.TRAINER_ID = t.TRAINER_ID
            LEFT JOIN DEPARTMENTS_TRAINING d
                ON c.DEPT_ID = d.DEPT_ID
            {whereSql}
            ORDER BY c.COURSE_ID DESC
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

        var courses = await connection.QueryAsync<TrainingCourse>(
            new CommandDefinition(
                querySql,
                parameters,
                cancellationToken: cancellationToken));

        return (courses.ToArray(), total);
    }

    public async Task<TrainingCourse?> GetByIdAsync(
        long courseId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = """
            SELECT
                c.COURSE_ID AS "CourseId",
                c.COURSE_NAME AS "CourseName",
                c.COURSE_TYPE AS "CourseType",
                c.DURATION_HOURS AS "DurationHours",
                c.TRAINER_ID AS "TrainerId",
                t.TRAINER_NAME AS "TrainerName",
                c.MAX_STUDENTS AS "MaxStudents",
                c.START_AT AS "StartAt",
                c.END_AT AS "EndAt",
                c.LOCATION AS "Location",
                c.COURSE_STATUS AS "CourseStatus",
                c.BUDGET_AMOUNT AS "BudgetAmount",
                c.DEPT_ID AS "DeptId",
                d.DEPT_NAME AS "DeptName",
                c.PRE_TEST_URL AS "PreTestUrl",
                c.POST_TEST_URL AS "PostTestUrl",
                c.MATERIAL_URL AS "MaterialUrl",
                c.CREATED_AT AS "CreatedAt",
                c.UPDATED_AT AS "UpdatedAt"
            FROM TRAINING_COURSES c
            LEFT JOIN TRAINERS t
                ON c.TRAINER_ID = t.TRAINER_ID
            LEFT JOIN DEPARTMENTS_TRAINING d
                ON c.DEPT_ID = d.DEPT_ID
            WHERE c.COURSE_ID = :CourseId
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<TrainingCourse>(
            new CommandDefinition(
                sql,
                new { CourseId = courseId },
                cancellationToken: cancellationToken));
    }

    public async Task<long> CreateAsync(
        TrainingCourse course,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO TRAINING_COURSES (
                COURSE_NAME,
                COURSE_TYPE,
                DURATION_HOURS,
                TRAINER_ID,
                MAX_STUDENTS,
                START_AT,
                END_AT,
                LOCATION,
                COURSE_STATUS,
                BUDGET_AMOUNT,
                DEPT_ID,
                PRE_TEST_URL,
                POST_TEST_URL,
                MATERIAL_URL,
                CREATED_AT,
                UPDATED_AT
            )
            VALUES (
                :CourseName,
                :CourseType,
                :DurationHours,
                :TrainerId,
                :MaxStudents,
                :StartAt,
                :EndAt,
                :Location,
                :CourseStatus,
                :BudgetAmount,
                :DeptId,
                :PreTestUrl,
                :PostTestUrl,
                :MaterialUrl,
                SYSTIMESTAMP,
                SYSTIMESTAMP
            )
            RETURNING COURSE_ID INTO :NewCourseId
            """;

        var parameters = new DynamicParameters();
        parameters.Add("CourseName", course.CourseName);
        parameters.Add("CourseType", course.CourseType);
        parameters.Add("DurationHours", course.DurationHours);
        parameters.Add("TrainerId", course.TrainerId);
        parameters.Add("MaxStudents", course.MaxStudents);
        parameters.Add("StartAt", course.StartAt);
        parameters.Add("EndAt", course.EndAt);
        parameters.Add("Location", course.Location);
        parameters.Add("CourseStatus", course.CourseStatus);
        parameters.Add("BudgetAmount", course.BudgetAmount);
        parameters.Add("DeptId", course.DeptId);
        parameters.Add("PreTestUrl", course.PreTestUrl);
        parameters.Add("PostTestUrl", course.PostTestUrl);
        parameters.Add("MaterialUrl", course.MaterialUrl);

        parameters.Add(
            "NewCourseId",
            dbType: DbType.Int64,
            direction: ParameterDirection.Output);

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                parameters,
                cancellationToken: cancellationToken));

        return parameters.Get<long>("NewCourseId");
    }

    public async Task<bool> UpdateAsync(
        TrainingCourse course,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE TRAINING_COURSES
            SET
                COURSE_NAME = :CourseName,
                COURSE_TYPE = :CourseType,
                DURATION_HOURS = :DurationHours,
                TRAINER_ID = :TrainerId,
                MAX_STUDENTS = :MaxStudents,
                START_AT = :StartAt,
                END_AT = :EndAt,
                LOCATION = :Location,
                BUDGET_AMOUNT = :BudgetAmount,
                DEPT_ID = :DeptId,
                PRE_TEST_URL = :PreTestUrl,
                POST_TEST_URL = :PostTestUrl,
                MATERIAL_URL = :MaterialUrl,
                UPDATED_AT = SYSTIMESTAMP
            WHERE COURSE_ID = :CourseId
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    course.CourseId,
                    course.CourseName,
                    course.CourseType,
                    course.DurationHours,
                    course.TrainerId,
                    course.MaxStudents,
                    course.StartAt,
                    course.EndAt,
                    course.Location,
                    course.BudgetAmount,
                    course.DeptId,
                    course.PreTestUrl,
                    course.PostTestUrl,
                    course.MaterialUrl
                },
                cancellationToken: cancellationToken));

        return affectedRows > 0;
    }

    public async Task<bool> UpdateStatusAsync(
        long courseId,
        string status,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE TRAINING_COURSES
            SET
                COURSE_STATUS = :Status,
                UPDATED_AT = SYSTIMESTAMP
            WHERE COURSE_ID = :CourseId
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    CourseId = courseId,
                    Status = status
                },
                cancellationToken: cancellationToken));

        return affectedRows > 0;
    }
}
