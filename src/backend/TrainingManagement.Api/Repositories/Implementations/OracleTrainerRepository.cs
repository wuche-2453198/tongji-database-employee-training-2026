using System.Data;
using Dapper;
using TrainingManagement.Api.Entities;
using TrainingManagement.Api.Repositories.Interfaces;

namespace TrainingManagement.Api.Repositories.Implementations;

public sealed class OracleTrainerRepository : ITrainerRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OracleTrainerRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Trainer>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return Array.Empty<Trainer>();
        }

        const string sql = """
            SELECT
                t.TRAINER_ID AS "TrainerId",
                t.TRAINER_NAME AS "TrainerName",
                t.TITLE AS "Title",
                t.COMPANY AS "Company",
                t.PHONE AS "Phone",
                t.EMAIL AS "Email",
                t.STAR_LEVEL AS "StarLevel",
                t.IS_INTERNAL AS "IsInternal",
                t.CREATED_AT AS "CreatedAt",
                t.UPDATED_AT AS "UpdatedAt"
            FROM TRAINERS t
            ORDER BY t.TRAINER_ID
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var trainers = await connection.QueryAsync<Trainer>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return trainers.ToArray();
    }

    public async Task<Trainer?> GetByIdAsync(
        long trainerId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return null;
        }

        const string sql = """
            SELECT
                t.TRAINER_ID AS "TrainerId",
                t.TRAINER_NAME AS "TrainerName",
                t.TITLE AS "Title",
                t.COMPANY AS "Company",
                t.PHONE AS "Phone",
                t.EMAIL AS "Email",
                t.STAR_LEVEL AS "StarLevel",
                t.IS_INTERNAL AS "IsInternal",
                t.CREATED_AT AS "CreatedAt",
                t.UPDATED_AT AS "UpdatedAt"
            FROM TRAINERS t
            WHERE t.TRAINER_ID = :TrainerId
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<Trainer>(
            new CommandDefinition(
                sql,
                new { TrainerId = trainerId },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> ExistsAsync(
        long trainerId,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return false;
        }

        const string sql = """
            SELECT COUNT(1)
            FROM TRAINERS t
            WHERE t.TRAINER_ID = :TrainerId
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { TrainerId = trainerId },
                cancellationToken: cancellationToken));

        return count > 0;
    }

    public async Task<long> CreateAsync(
        Trainer trainer,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO TRAINERS (
                TRAINER_NAME,
                TITLE,
                COMPANY,
                PHONE,
                EMAIL,
                STAR_LEVEL,
                IS_INTERNAL,
                CREATED_AT,
                UPDATED_AT
            )
            VALUES (
                :TrainerName,
                :Title,
                :Company,
                :Phone,
                :Email,
                :StarLevel,
                :IsInternal,
                SYSTIMESTAMP,
                SYSTIMESTAMP
            )
            RETURNING TRAINER_ID INTO :NewTrainerId
            """;

        var parameters = new DynamicParameters();
        parameters.Add("TrainerName", trainer.TrainerName);
        parameters.Add("Title", trainer.Title);
        parameters.Add("Company", trainer.Company);
        parameters.Add("Phone", trainer.Phone);
        parameters.Add("Email", trainer.Email);
        parameters.Add("StarLevel", trainer.StarLevel);
        parameters.Add("IsInternal", trainer.IsInternal);
        parameters.Add("NewTrainerId", dbType: DbType.Int64, direction: ParameterDirection.Output);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return parameters.Get<long>("NewTrainerId");
    }

    public async Task<bool> UpdateAsync(
        Trainer trainer,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE TRAINERS
            SET
                TRAINER_NAME = :TrainerName,
                TITLE = :Title,
                COMPANY = :Company,
                PHONE = :Phone,
                EMAIL = :Email,
                STAR_LEVEL = :StarLevel,
                IS_INTERNAL = :IsInternal,
                UPDATED_AT = SYSTIMESTAMP
            WHERE TRAINER_ID = :TrainerId
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var affectedRows = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new
                {
                    trainer.TrainerId,
                    trainer.TrainerName,
                    trainer.Title,
                    trainer.Company,
                    trainer.Phone,
                    trainer.Email,
                    trainer.StarLevel,
                    trainer.IsInternal
                },
                cancellationToken: cancellationToken));

        return affectedRows > 0;
    }
}