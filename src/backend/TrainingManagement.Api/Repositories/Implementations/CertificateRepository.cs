using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;
public sealed class CertificateRepository : ICertificateRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CertificateRepository(IDbConnectionFactory connectionFactory)
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
            SELECT CERT_ID AS CertId, EMP_ID AS EmpId, COURSE_ID AS CourseId,
                   CERT_CODE AS CertCode, ISSUE_DATE AS IssueDate, EXPIRE_DATE AS ExpireDate,
                   NOTIFIED AS Notified, NOTIFIED_AT AS NotifiedAt,
                   ISSUED_BY_EMP_ID AS IssuedByEmpId, CREATED_AT AS CreatedAt
            FROM TRAINING_CERTIFICATES
            WHERE EMP_ID = :EmpId
            ORDER BY ISSUE_DATE DESC
            """;
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.QueryAsync<TrainingCertificate>(sql, new { EmpId = employeeId });
    }

    public async Task<TrainingCertificate?> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT CERT_ID AS CertId, EMP_ID AS EmpId, COURSE_ID AS CourseId,
                   CERT_CODE AS CertCode, ISSUE_DATE AS IssueDate, EXPIRE_DATE AS ExpireDate,
                   NOTIFIED AS Notified, NOTIFIED_AT AS NotifiedAt,
                   ISSUED_BY_EMP_ID AS IssuedByEmpId, CREATED_AT AS CreatedAt
            FROM TRAINING_CERTIFICATES
            WHERE CERT_ID = :CertId
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
}
