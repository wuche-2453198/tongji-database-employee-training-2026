using System.Data;
using Dapper;
using TrainingManagement.Api.Dtos.Trainer;
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

    public async Task<IReadOnlyList<Trainer>> GetAllAsync(
        TrainerQuery query,
        CancellationToken cancellationToken)
    {
        if (!_connectionFactory.IsConfigured)
        {
            return Array.Empty<Trainer>();
        }

        var conditions = new List<string>();
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(query.TrainerName))
        {
            conditions.Add(
                "TRAINER_NAME LIKE :TrainerName");

            parameters.Add(
                "TrainerName",
                $"%{query.TrainerName}%");
        }

        if (!string.IsNullOrWhiteSpace(query.Company))
        {
            conditions.Add(
                "COMPANY LIKE :Company");

            parameters.Add(
                "Company",
                $"%{query.Company}%");
        }

        if (!string.IsNullOrWhiteSpace(query.IsInternal))
        {
            conditions.Add(
                "IS_INTERNAL = :IsInternal");

            parameters.Add(
                "IsInternal",
                query.IsInternal);
        }

        var whereSql = conditions.Count == 0
            ? string.Empty
            : "WHERE " + string.Join(" AND ", conditions);

        var sql = $"""
            SELECT
                TRAINER_ID AS "TrainerId",
                TRAINER_NAME AS "TrainerName",
                TITLE AS "Title",
                COMPANY AS "Company",
                PHONE AS "Phone",
                EMAIL AS "Email",
                STAR_LEVEL AS "StarLevel",
                IS_INTERNAL AS "IsInternal",
                CREATED_AT AS "CreatedAt",
                UPDATED_AT AS "UpdatedAt"
            FROM TRAINERS
            {whereSql}
            ORDER BY TRAINER_ID DESC
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        var trainers = await connection.QueryAsync<Trainer>(
            new CommandDefinition(
                sql,
                parameters,
                cancellationToken: cancellationToken));

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
                TRAINER_ID AS "TrainerId",
                TRAINER_NAME AS "TrainerName",
                TITLE AS "Title",
                COMPANY AS "Company",
                PHONE AS "Phone",
                EMAIL AS "Email",
                STAR_LEVEL AS "StarLevel",
                IS_INTERNAL AS "IsInternal",
                CREATED_AT AS "CreatedAt",
                UPDATED_AT AS "UpdatedAt"
            FROM TRAINERS
            WHERE TRAINER_ID = :TrainerId
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Trainer>(
            new CommandDefinition(
                sql,
                new
                {
                    TrainerId = trainerId
                },
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
            FROM TRAINERS
            WHERE TRAINER_ID = :TrainerId
            """;

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

        var count = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                sql,
                new
                {
                    TrainerId = trainerId
                },
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

        parameters.Add(
            "TrainerName",
            trainer.TrainerName);

        parameters.Add(
            "Title",
            trainer.Title);

        parameters.Add(
            "Company",
            trainer.Company);

        parameters.Add(
            "Phone",
            trainer.Phone);

        parameters.Add(
            "Email",
            trainer.Email);

        parameters.Add(
            "StarLevel",
            trainer.StarLevel);

        parameters.Add(
            "IsInternal",
            trainer.IsInternal);

        parameters.Add(
            "NewTrainerId",
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

        return parameters.Get<long>(
            "NewTrainerId");
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

        await using var connection =
            await _connectionFactory.CreateOpenConnectionAsync(
                cancellationToken);

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
