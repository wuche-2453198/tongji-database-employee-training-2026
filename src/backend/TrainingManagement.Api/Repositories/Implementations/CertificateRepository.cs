using Dapper;
using System.Data;
using System.Threading.Tasks;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations
{
    public class CertificateRepository : ICertificateRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;

        public CertificateRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<object> GetRegistrationByIdAsync(int registrationId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT REGISTRATION_ID, EMPLOYEE_ID, COURSE_ID, STATUS FROM TRAINING_REGISTRATIONS WHERE REGISTRATION_ID = :RegistrationId";
            return await connection.QueryFirstOrDefaultAsync(sql, new { RegistrationId = registrationId });
        }

        public async Task<bool> ExistsByEmployeeAndCourseAsync(int employeeId, int courseId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT COUNT(1) FROM TRAINING_CERTIFICATES WHERE EMPLOYEE_ID = :EmployeeId AND COURSE_ID = :CourseId AND STATUS = 'ACTIVE'";
            var count = await connection.ExecuteScalarAsync<int>(sql, new { EmployeeId = employeeId, CourseId = courseId });
            return count > 0;
        }

        public async Task<int> CreateAsync(string certificateNo, int employeeId, int courseId, int registrationId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = @"INSERT INTO TRAINING_CERTIFICATES 
                        (CERTIFICATE_NO, EMPLOYEE_ID, COURSE_ID, REGISTRATION_ID, ISSUE_DATE, STATUS, NOTIFY_FLAG) 
                        VALUES (:CertificateNo, :EmployeeId, :CourseId, :RegistrationId, SYSDATE, 'ACTIVE', 'N')
                        RETURNING CERTIFICATE_ID INTO :CertificateId";

            var parameters = new DynamicParameters();
            parameters.Add("CertificateNo", certificateNo);
            parameters.Add("EmployeeId", employeeId);
            parameters.Add("CourseId", courseId);
            parameters.Add("RegistrationId", registrationId);
            parameters.Add("CertificateId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(sql, parameters);
            return parameters.Get<int>("CertificateId");
        }

        public async Task<object> GetByEmployeeIdAsync(int employeeId)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT * FROM TRAINING_CERTIFICATES WHERE EMPLOYEE_ID = :EmployeeId ORDER BY ISSUE_DATE DESC";
            return await connection.QueryAsync(sql, new { EmployeeId = employeeId });
        }

        public async Task<object> GetByIdAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "SELECT * FROM TRAINING_CERTIFICATES WHERE CERTIFICATE_ID = :CertificateId";
            return await connection.QueryFirstOrDefaultAsync(sql, new { CertificateId = id });
        }

        public async Task<bool> UpdateNotifyFlagAsync(int id)
        {
            using var connection = _connectionFactory.CreateConnection();
            var sql = "UPDATE TRAINING_CERTIFICATES SET NOTIFY_FLAG = 'Y' WHERE CERTIFICATE_ID = :CertificateId";
            var result = await connection.ExecuteAsync(sql, new { CertificateId = id });
            return result > 0;
        }
    }
}
