using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using TrainingManagement.Api.Common;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations
{
    public class TrainingRequestRepository: ITrainingRequestRepository
    {
        private readonly OracleConnectionFactory _connectionFactory;
        public TrainingRequestRepository(OracleConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<int> InsertAsync(TrainingRequest request)
        {
            const string sql = @"INSERT INTO TRAINING_REQUESTS
                               (EMP_ID, COURSE_ID, REASON, STATUS)
                               VALUES(:EmpId, :CourseId, :Reason, :Status)
                               RETURNING REQUEST_ID INTO :NewId";
            using var connection = _connectionFactory.CreateConnection();
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
            const string sql = @"SELECT * FROM TRAINING_REQUESTS
                                WHERE REQUEST_ID = :Id";
            using var connection = _connectionFactory.CreateConnection();
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
                conditions.Add("STATUS = :Status");
                parameters.Add(new OracleParameter("Status", status));
            }

            if (employeeId.HasValue)
            {
                conditions.Add("EMP_ID = :EmployeeId");
                parameters.Add(new OracleParameter("EmployeeId", employeeId.Value));
            }

            if (courseId.HasValue)
            {
                conditions.Add("COURSE_ID = :CourseId");
                parameters.Add(new OracleParameter("CourseId", courseId.Value));
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            int offset = (page - 1) * pageSize;

            var sql = $@"
                         SELECT * FROM TRAINING_REQUESTS
                         {whereClause}
                         ORDER BY REQUESTED_AT DESC
                         OFFSET :Offset ROWS FETCH NEXT :PageSize ROWS ONLY";

            parameters.Add(new OracleParameter("Offset", offset));
            parameters.Add(new OracleParameter("PageSize", pageSize));

            var items = new List<TrainingRequest>();

            using var connection = _connectionFactory.CreateConnection();
            using var command = new OracleCommand(sql, (OracleConnection)connection);
            command.Parameters.AddRange(parameters.ToArray());

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(MapToEntity(reader));
            }

            var countSql = $"SELECT COUNT(*) FROM TRAINING_REQUESTS {whereClause}";

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
                        WHERE REQUEST_ID = :Id";
                    break;

                case "DEPT_REJECTED":
                    sql = @"
                    UPDATE TRAINING_REQUESTS
                    SET STATUS = :Status,
                        DEPT_APPROVER_EMP_ID = :ApproverId,
                        DEPT_APPROVED_AT = SYSTIMESTAMP,
                        DEPT_APPROVE_REMARK = :ApproveRemark,
                        UPDATED_AT = SYSTIMESTAMP
                        WHERE REQUEST_ID = :Id";
                    break;

                case "HR_FILED":
                    sql = @"
                    UPDATE TRAINING_REQUESTS
                    SET STATUS = :Status,
                        HR_FILER_EMP_ID = :ApproverId,
                        HR_FILED_AT = SYSTIMESTAMP,
                        UPDATED_AT = SYSTIMESTAMP
                        WHERE REQUEST_ID = :Id";
                    break;

                default:
                    return false;
            }

            using var connection = _connectionFactory.CreateConnection();
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

        public async Task<bool> ExistsPendingRequestAsvnc(int employeeId, int courseId)
        {
            const string sql = @"SELECT * FROM TRAINING_REQUESTS
                                WHERE EMP_ID = :EmployeeId AND COURSE_ID = :CourseID
                                               AND STATUS IN ('PENDING', 'DEPT_APPROVED')";
            using var connection = _connectionFactory.CreateConnection();
            using var command = new OracleCommand(sql, (OracleConnection)connection);
            command.Parameters.Add(new OracleParameter("EmployeeId", employeeId));
            command.Parameters.Add(new OracleParameter("CourseID", courseId));

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
                Id = reader.GetInt32(reader.GetOrdinal("REQUEST_ID")),
                EmployeeId = reader.GetInt32(reader.GetOrdinal("EMP_ID")),
                CourseId = reader.GetInt32(reader.GetOrdinal("COURSE_ID")),
                RequestReason = reader.GetString(reader.GetOrdinal("REASON")),
                Status = reader.GetString(reader.GetOrdinal("STATUS")),
                CreateTime = reader.GetDateTime(reader.GetOrdinal("REQUESTED_AT")),

                DeptApproverId = reader.IsDBNull(reader.GetOrdinal("DEPT_APPROVER_EMP_ID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("DEPT_APPROVER_EMP_ID")),

                DeptApproveTime = reader.IsDBNull(reader.GetOrdinal("DEPT_APPROVED_AT")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("DEPT_APPROVED_AT")),

                DeptApproveComment = reader.IsDBNull(reader.GetOrdinal("DEPT_APPROVE_REMARK")) ? null : reader.GetString(reader.GetOrdinal("DEPT_APPROVE_REMARK")),

                HrApproverId = reader.IsDBNull(reader.GetOrdinal("HR_FILER_EMP_ID")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("HR_FILER_EMP_ID")),

                HrFileTime = reader.IsDBNull(reader.GetOrdinal("HR_FILED_AT")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("HR_FILED_AT"))
            };
        }
    }
}
