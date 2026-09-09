using System.Data;
using Dapper;
using Oracle.ManagedDataAccess.Client;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class OracleAttendanceRepository : IAttendanceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleAttendanceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SignInResult> SignInAsync(
        AttendanceRecord attendance,
        decimal actualHours,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return new SignInResult(
                SignInOutcome.RegistrationConflict);
        }

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var affectedRows = await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                    UPDATE TRAINING_REGISTRATIONS
                    SET
                        STATUS = 'SIGNED_IN',
                        ACTUAL_HOURS = :ActualHours,
                        UPDATED_AT = SYSTIMESTAMP
                    WHERE REG_ID = :RegId
                      AND STATUS = 'REGISTERED'
                    """,
                    new
                    {
                        RegId = attendance.RegId,
                        ActualHours = actualHours
                    },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

            if (affectedRows == 0)
            {
                await transaction.RollbackAsync(cancellationToken);

                return new SignInResult(
                    SignInOutcome.RegistrationConflict);
            }

            var parameters = new DynamicParameters();

            parameters.Add(
                "RegId",
                attendance.RegId);

            parameters.Add(
                "SigninType",
                attendance.SigninType);

            parameters.Add(
                "SignedInAt",
                attendance.SignedInAt);

            parameters.Add(
                "LatenessMinutes",
                attendance.LatenessMinutes);

            parameters.Add(
                "DeductHours",
                attendance.DeductHours);

            parameters.Add(
                "Remark",
                attendance.Remark);

            parameters.Add(
                "NewAttendId",
                dbType: DbType.Int64,
                direction: ParameterDirection.Output);

            try
            {
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        """
                        INSERT INTO TRAINING_ATTENDANCE (
                            REG_ID,
                            SIGNIN_TYPE,
                            SIGNED_IN_AT,
                            LATENESS_MINUTES,
                            DEDUCT_HOURS,
                            REMARK,
                            CREATED_AT
                        )
                        VALUES (
                            :RegId,
                            :SigninType,
                            :SignedInAt,
                            :LatenessMinutes,
                            :DeductHours,
                            :Remark,
                            SYSTIMESTAMP
                        )
                        RETURNING ATTEND_ID INTO :NewAttendId
                        """,
                        parameters,
                        transaction: transaction,
                        cancellationToken: cancellationToken));
            }
            catch (OracleException exception)
                when (exception.Number == 1)
            {
                await transaction.RollbackAsync(cancellationToken);

                return new SignInResult(
                    SignInOutcome.DuplicateAttendance);
            }

            await transaction.CommitAsync(cancellationToken);

            return new SignInResult(
                SignInOutcome.Success,
                parameters.Get<long>("NewAttendId"));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
