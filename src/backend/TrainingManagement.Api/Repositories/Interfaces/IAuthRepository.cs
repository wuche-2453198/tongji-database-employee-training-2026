using TrainingManagement.Api.Entities;

namespace TrainingManagement.Api.Repositories.Interfaces;

public interface IAuthRepository
{
    /// <summary>按登录名、编号、邮箱、电话或姓名参数化查询员工，未匹配时返回空。</summary>
    Task<EmployeeAuthRecord?> FindEmployeeForLoginAsync(
        string identifier,
        CancellationToken cancellationToken);

    /// <summary>按真实员工主键读取认证所需资料及部门名称。</summary>
    Task<EmployeeAuthRecord?> GetEmployeeByIdAsync(
        long empId,
        CancellationToken cancellationToken);

    /// <summary>通过用户角色关联表读取员工角色；当前表结构不存储权限列表，交给服务层映射。</summary>
    Task<IReadOnlyCollection<RoleRecord>> GetRolesByEmployeeIdAsync(
        long empId,
        CancellationToken cancellationToken);
}
