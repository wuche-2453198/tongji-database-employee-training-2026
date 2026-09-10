using System.Data;
using Dapper;
using TrainingManagement.Api.Common.Security;
using TrainingManagement.Api.Dtos.Dashboard;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DashboardRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(int employeeId, string role, CancellationToken cancellationToken)
    {
        var stats = new DashboardStatsDto();
        if (!_connectionFactory.IsConfigured)
        {
            return stats;
        }

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        if (role == RoleCodes.DepartmentManager)
        {
            stats.PendingApprovals = await CountAsync(connection, """
                SELECT COUNT(1)
                FROM TRAINING_REQUESTS r
                JOIN EMPLOYEES e ON r.EMP_ID = e.EMP_ID
                WHERE e.DEPT_ID = (SELECT DEPT_ID FROM EMPLOYEES WHERE EMP_ID = :EmpId)
                  AND r.STATUS = 'PENDING'
                """, new { EmpId = employeeId }, cancellationToken);

            stats.HandledThisWeek = await CountAsync(connection, """
                SELECT COUNT(1)
                FROM TRAINING_REQUESTS r
                JOIN EMPLOYEES e ON r.EMP_ID = e.EMP_ID
                WHERE e.DEPT_ID = (SELECT DEPT_ID FROM EMPLOYEES WHERE EMP_ID = :EmpId)
                  AND r.STATUS IN ('DEPT_APPROVED', 'DEPT_REJECTED')
                  AND r.DEPT_APPROVED_AT >= TRUNC(SYSDATE, 'IW')
                """, new { EmpId = employeeId }, cancellationToken);

            stats.OverdueRequests = await CountAsync(connection, """
                SELECT COUNT(1)
                FROM TRAINING_REQUESTS r
                JOIN EMPLOYEES e ON r.EMP_ID = e.EMP_ID
                JOIN TRAINING_COURSES c ON r.COURSE_ID = c.COURSE_ID
                WHERE e.DEPT_ID = (SELECT DEPT_ID FROM EMPLOYEES WHERE EMP_ID = :EmpId)
                  AND r.STATUS = 'PENDING'
                  AND c.START_AT < SYSDATE
                """, new { EmpId = employeeId }, cancellationToken);
        }
        else if (role == RoleCodes.Hr)
        {
            stats.PendingFilings = await CountAsync(connection, """
                SELECT COUNT(1) FROM TRAINING_REQUESTS WHERE STATUS = 'DEPT_APPROVED'
                """, null, cancellationToken);

            stats.PendingSignIn = await CountAsync(connection, """
                SELECT COUNT(1) FROM TRAINING_REGISTRATIONS WHERE STATUS = 'REGISTERED'
                """, null, cancellationToken);

            stats.PendingCertificates = await CountPendingCertificatesAsync(connection, cancellationToken);
        }
        else if (role == RoleCodes.Admin)
        {
            stats.TotalCourses = await CountAsync(connection, """
                SELECT COUNT(1) FROM TRAINING_COURSES
                """, null, cancellationToken);

            stats.PendingApprovals = await CountAsync(connection, """
                SELECT COUNT(1) FROM TRAINING_REQUESTS WHERE STATUS = 'PENDING'
                """, null, cancellationToken);

            stats.PendingCertificates = await CountPendingCertificatesAsync(connection, cancellationToken);
        }
        else
        {
            stats.UpcomingCourses = await CountAsync(connection, """
                SELECT COUNT(1)
                FROM TRAINING_COURSES
                WHERE COURSE_STATUS = 'PUBLISHED' AND START_AT > SYSDATE
                """, null, cancellationToken);

            stats.ActiveRequests = await CountAsync(connection, """
                SELECT COUNT(1)
                FROM TRAINING_REQUESTS
                WHERE EMP_ID = :EmpId AND STATUS IN ('PENDING', 'DEPT_APPROVED')
                """, new { EmpId = employeeId }, cancellationToken);

            stats.ValidCertificates = await CountAsync(connection, """
                SELECT COUNT(1)
                FROM TRAINING_CERTIFICATES
                WHERE EMP_ID = :EmpId AND (EXPIRE_DATE IS NULL OR EXPIRE_DATE > SYSDATE)
                """, new { EmpId = employeeId }, cancellationToken);
        }

        return stats;
    }

    private static Task<int> CountPendingCertificatesAsync(IDbConnection connection, CancellationToken cancellationToken)
    {
        return CountAsync(connection, """
            SELECT COUNT(1)
            FROM TRAINING_REGISTRATIONS r
            WHERE r.STATUS = 'COMPLETED'
              AND NOT EXISTS (
                  SELECT 1 FROM TRAINING_CERTIFICATES cert
                  WHERE cert.EMP_ID = r.EMP_ID AND cert.COURSE_ID = r.COURSE_ID
              )
            """, null, cancellationToken);
    }

    private static Task<int> CountAsync(
        IDbConnection connection,
        string sql,
        object? param,
        CancellationToken cancellationToken)
    {
        return connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, param, cancellationToken: cancellationToken));
    }
}
