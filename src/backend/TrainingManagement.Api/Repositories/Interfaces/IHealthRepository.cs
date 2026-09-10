namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IHealthRepository
{
    bool IsDatabaseConfigured { get; }

    /// <summary>执行 SELECT 1 FROM DUAL 验证数据库连接和基本查询能力，不代表所有业务表均可访问。</summary>
    Task CheckDatabaseAsync(CancellationToken cancellationToken);
}
