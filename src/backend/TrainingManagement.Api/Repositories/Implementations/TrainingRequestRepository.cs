using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;
using System.Data.Common;
using System.Threading;

namespace TrainingManagement.Api.Repositories.Implementations
{
    public class TrainingRequestRepository: ITrainingRequestRepository
    {
        private readonly IDbConnectionFactory _connectionFactory;
        public TrainingRequestRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> InsertAsync(TrainingRequest request)
        {
            const string sql = @"INSERT INTO TRAINING_REQUESTS
                               (EMP_ID, COURSE_ID, REASON, STATUS)
                               VALUES(:EmpId, :CourseId, :Reason, :Status)
                               RETURNING REQUEST_ID INTO :NewId";
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
            using var command = new OracleCommand(sql, (OracleConnection)connection);

            command.Parameters.Add(new OracleParameter("EmpId", request.EmployeeId));
            command.Parameters.Add(new OracleParameter("CourseId", request.CourseId));
            command.Parameters.Add(new OracleParameter("Reason", request.RequestReason));
            command.Parameters.Add(new OracleParameter("Status", "PENDING"));

            var returnParameter = new OracleParameter { 
                ParameterName = "NewId",
                OracleDbType = OracleDbType.Int32,
                Direction = System.Data.ParameterDirection.ReturnValue
            };
            command.Parameters.Add(returnParameter);

            await command.ExecuteNonQueryAsync();

            return ((OracleDecimal)returnParameter.Value).ToInt32();
        }

        public async Task<TrainingRequest?> GetByIdAsync(int id)
        {
            const string sql = @"SELECT 
                                    r.REQUEST_ID AS Id,
                                    r.EMP_ID AS EmployeeId,
                                    e.EMP_NAME AS EmployeeName,
                                    r.COURSE_ID AS CourseId,
                                    c.COURSE_NAME AS CourseName,
                                    r.REASON AS RequestReason,
                                    r.STATUS AS Status,
                                    r.REQUESTED_AT AS CreateTime,
                                    r.DEPT_APPROVER_EMP_ID AS DeptApproverId,
                                    a.EMP_NAME AS DeptApproverName,
                                    r.DEPT_APPROVED_AT AS DeptApproveTime,
                                    r.DEPT_APPROVE_REMARK AS DeptApproveComment,
                                    r.HR_FILER_EMP_ID AS HrApproverId,
                                    h.EMP_NAME AS HrApproverName,
                                    r.HR_FILED_AT AS HrFileTime
                                 FROM TRAINING_REQUESTS r
                                    LEFT JOIN EMPLOYEES e ON r.EMP_ID = e.EMP_ID
                                    LEFT JOIN TRAINING_COURSES c ON r.COURSE_ID = c.COURSE_ID
                                    LEFT JOIN EMPLOYEES a ON r.DEPT_APPROVER_EMP_ID = a.EMP_ID
                                    LEFT JOIN EMPLOYEES h ON r.HR_FILER_EMP_ID = h.EMP_ID
                                 WHERE r.REQUEST_ID = :Id";

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
            using var command = new OracleCommand(sql, (OracleConnection)connection);
            command.Parameters.Add(new OracleParameter("Id", id));

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapToEntity(reader);
            }

            return null;
        }

        public async Task<(List<TrainingRequest> Items, int Total)> GetListAsync(string? status, int? employeeId, int? courseId, int page, int pageSize)
        {
            // 1. 构建动态 WHERE 条件
            var conditions = new List<string>();
            var parameters = new List<OracleParameter>();

            if (!string.IsNullOrEmpty(status))
            {
                conditions.Add("r.STATUS = :Status");
                parameters.Add(new OracleParameter("Status", status));
            }

            if (employeeId.HasValue)
            {
                conditions.Add("r.EMP_ID = :EmployeeId");
                parameters.Add(new OracleParameter("EmployeeId", employeeId.Value));
            }

            if (courseId.HasValue)
            {
                conditions.Add("r.COURSE_ID = :CourseId");
                parameters.Add(new OracleParameter("CourseId", courseId.Value));
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            int offset = (page - 1) * pageSize;

            var sql = $@"SELECT 
                            r.REQUEST_ID AS Id,
                            r.EMP_ID AS EmployeeId,
                            e.EMP_NAME AS EmployeeName,
                            r.COURSE_ID AS CourseId,
                            c.COURSE_NAME AS CourseName,
                            r.REASON AS RequestReason,
                            r.STATUS AS Status,
                            r.REQUESTED_AT AS CreateTime,
                            r.DEPT_APPROVER_EMP_ID AS DeptApproverId,
                            a.EMP_NAME AS DeptApproverName,
                            r.DEPT_APPROVED_AT AS DeptApproveTime,
                            r.DEPT_APPROVE_REMARK AS DeptApproveComment,
                            r.HR_FILER_EMP_ID AS HrApproverId,
                            h.EMP_NAME AS HrApproverName,
                            r.HR_FILED_AT AS HrFileTime
                         FROM TRAINING_REQUESTS r
                            LEFT JOIN EMPLOYEES e ON r.EMP_ID = e.EMP_ID
                            LEFT JOIN TRAINING_COURSES c ON r.COURSE_ID = c.COURSE_ID
                            LEFT JOIN EMPLOYEES a ON r.DEPT_APPROVER_EMP_ID = a.EMP_ID
                            LEFT JOIN EMPLOYEES h ON r.HR_FILER_EMP_ID = h.EMP_ID
                         {whereClause}
                         ORDER BY r.REQUESTED_AT DESC
                         OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

            parameters.Add(new OracleParameter("Offset", offset));
            parameters.Add(new OracleParameter("PageSize", pageSize));

            var items = new List<TrainingRequest>();

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
            using var command = new OracleCommand(sql, (OracleConnection)connection);
            command.Parameters.AddRange(parameters.ToArray());

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapToEntity(reader));
            }

            var countSql = $"SELECT COUNT(*) FROM TRAINING_REQUESTS r {whereClause}";

            using var countCommand = new OracleCommand(countSql, (OracleConnection)connection);

            var filterParameters = parameters
                .Where(p => p.ParameterName != "Offset" && p.ParameterName != "PageSize")
                .ToArray();
            countCommand.Parameters.AddRange(filterParameters);

            var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

            return (items, total);
        }

        public async Task<bool> UpdateStatusAsync(int id, string newStatus, int? approverId, string? comment)
        {
            string sql;

            switch (newStatus)
            {
                case "DEPT_APPROVED":
                    sql = @"
                    UPDATE TRAINING_REQUESTS
                    SET STATUS = :Status,
                        DEPT_APPROVER_EMP_ID = :ApproverId,
                        DEPT_APPROVED_AT = SYSTIMESTAMP,
                        DEPT_APPROVE_REMARK = :ApproveRemark,
                        UPDATED_AT = SYSTIMESTAMP
                        WHERE REQUEST_ID = :Id AND STATUS = 'PENDING'";
                    break;

                case "DEPT_REJECTED":
                    sql = @"
                    UPDATE TRAINING_REQUESTS
                    SET STATUS = :Status,
                        DEPT_APPROVER_EMP_ID = :ApproverId,
                        DEPT_APPROVED_AT = SYSTIMESTAMP,
                        DEPT_APPROVE_REMARK = :ApproveRemark,
                        UPDATED_AT = SYSTIMESTAMP
                        WHERE REQUEST_ID = :Id AND STATUS = 'PENDING'";
                    break;

                case "HR_FILED":
                    sql = @"
                    UPDATE TRAINING_REQUESTS
                    SET STATUS = :Status,
                        HR_FILER_EMP_ID = :ApproverId,
                        HR_FILED_AT = SYSTIMESTAMP,
                        UPDATED_AT = SYSTIMESTAMP
                        WHERE REQUEST_ID = :Id AND STATUS = 'DEPT_APPROVED'";
                    break;

                default:
                    return false;
            }

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
            using var command = new OracleCommand(sql, (OracleConnection)connection);

            command.Parameters.Add(new OracleParameter("Status", newStatus));
            command.Parameters.Add(new OracleParameter("ApproverId", approverId ?? (object)DBNull.Value));

            // 只有 SQL 中包含 :comment 时才添加
            if (newStatus == "DEPT_APPROVED" || newStatus == "DEPT_REJECTED")
            {
                command.Parameters.Add(new OracleParameter("ApproveRemark", comment ?? (object)DBNull.Value));
            }

            command.Parameters.Add(new OracleParameter("Id", id));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> ExistsPendingRequestAsync(int employeeId, int courseId)
        {
            const string sql = @"SELECT * FROM TRAINING_REQUESTS
                                WHERE EMP_ID = :EmployeeId AND COURSE_ID = :CourseId
                                               AND STATUS IN ('PENDING', 'DEPT_APPROVED')";
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(CancellationToken.None);
            using var command = new OracleCommand(sql, (OracleConnection)connection);
            command.Parameters.Add(new OracleParameter("EmployeeId", employeeId));
            command.Parameters.Add(new OracleParameter("CourseId", courseId));

            using var reader = await command.ExecuteReaderAsync();
            if(await reader.ReadAsync())
            {
                return true;
            }

            return false;
        }

        private TrainingRequest MapToEntity(OracleDataReader reader)
        {
            return new TrainingRequest
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                EmployeeId = reader.GetInt32(reader.GetOrdinal("EmployeeId")),
                CourseId = reader.GetInt32(reader.GetOrdinal("CourseId")),
                RequestReason = reader.GetString(reader.GetOrdinal("RequestReason")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreateTime = reader.GetDateTime(reader.GetOrdinal("CreateTime")),
                DeptApproverId = reader.IsDBNull(reader.GetOrdinal("DeptApproverId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("DeptApproverId")),
                DeptApproveTime = reader.IsDBNull(reader.GetOrdinal("DeptApproveTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DeptApproveTime")),
                DeptApproveComment = reader.IsDBNull(reader.GetOrdinal("DeptApproveComment")) ? null : reader.GetString(reader.GetOrdinal("DeptApproveComment")),
                HrApproverId = reader.IsDBNull(reader.GetOrdinal("HrApproverId")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("HrApproverId")),
                HrFileTime = reader.IsDBNull(reader.GetOrdinal("HrFileTime")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("HrFileTime")),
                EmployeeName = reader.IsDBNull(reader.GetOrdinal("EmployeeName")) ? "未知" : reader.GetString(reader.GetOrdinal("EmployeeName")),
                CourseName = reader.IsDBNull(reader.GetOrdinal("CourseName")) ? "未知" : reader.GetString(reader.GetOrdinal("CourseName")),
                DeptApproverName = reader.IsDBNull(reader.GetOrdinal("DeptApproverName")) ? null : reader.GetString(reader.GetOrdinal("DeptApproverName")),
                HrApproverName = reader.IsDBNull(reader.GetOrdinal("HrApproverName")) ? null : reader.GetString(reader.GetOrdinal("HrApproverName"))
            };
        }
    }
}
