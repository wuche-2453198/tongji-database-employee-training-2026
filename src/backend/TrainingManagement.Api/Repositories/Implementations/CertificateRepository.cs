using Dapper;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class CertificateRepository : ICertificateRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CertificateRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<object> GetRegistrationByIdAsync(int registrationId)
    {
        const string sql = """
            SELECT REGISTRATION_ID, EMPLOYEE_ID, COURSE_ID, STATUS 
            FROM TRAINING_REGISTRATIONS 
            WHERE REGISTRATION_ID = :RegistrationId
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.QueryFirstOrDefaultAsync(sql, new { RegistrationId = registrationId });
    }

    public async Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId)
    {
        const string sql = """
            SELECT COUNT(1) FROM TRAINING_CERTIFICATES 
            WHERE EMPLOYEE_ID = :EmployeeId AND COURSE_ID = :CourseId AND STATUS = 'ACTIVE'
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var count = await connection.ExecuteScalarAsync<int>(sql, new { EmployeeId = employeeId, CourseId = courseId });
        return count > 0;
    }

    public async Task<int> CreateAsync(string certificateNo, int employeeId, int courseId, int registrationId)
    {
        const string sql = """
            INSERT INTO TRAINING_CERTIFICATES 
                (CERTIFICATE_NO, EMPLOYEE_ID, COURSE_ID, REGISTRATION_ID, ISSUE_DATE, STATUS, NOTIFY_FLAG) 
            VALUES 
                (:CertificateNo, :EmployeeId, :CourseId, :RegistrationId, SYSDATE, 'ACTIVE', 'N')
            RETURNING CERTIFICATE_ID INTO :CertificateId
            """;

        var parameters = new DynamicParameters();
        parameters.Add("CertificateNo", certificateNo);
        parameters.Add("EmployeeId", employeeId);
        parameters.Add("CourseId", courseId);
        parameters.Add("RegistrationId", registrationId);
        parameters.Add("CertificateId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        await connection.ExecuteAsync(sql, parameters);
        return parameters.Get<int>("CertificateId");
    }

    public async Task<object> GetByEmployeeIdAsync(int employeeId)
    {
        const string sql = """
            SELECT * FROM TRAINING_CERTIFICATES 
            WHERE EMPLOYEE_ID = :EmployeeId 
            ORDER BY ISSUE_DATE DESC
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.QueryAsync(sql, new { EmployeeId = employeeId });
    }

    public async Task<object> GetByIdAsync(int id)
    {
        const string sql = """
            SELECT * FROM TRAINING_CERTIFICATES WHERE CERTIFICATE_ID = :CertificateId
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        return await connection.QueryFirstOrDefaultAsync(sql, new { CertificateId = id });
    }

    public async Task<bool> UpdateNotifyFlagAsync(int id)
    {
        const string sql = """
            UPDATE TRAINING_CERTIFICATES SET NOTIFY_FLAG = 'Y' WHERE CERTIFICATE_ID = :CertificateId
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
        var rows = await connection.ExecuteAsync(sql, new { CertificateId = id });
        return rows > 0;
    }
}
