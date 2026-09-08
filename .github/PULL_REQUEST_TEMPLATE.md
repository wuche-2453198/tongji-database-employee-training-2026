## 变更说明

<!-- 简述本次变更解决的问题和范围。 -->

## 影响范围

- [ ] 后端
- [ ] 前端
- [ ] 数据库迁移/种子
- [ ] OpenAPI/DTO
- [ ] 文档
- [ ] 测试

## 验证结果

- [ ] 后端构建或测试通过
- [ ] 前端构建或页面验证通过
- [ ] 数据库迁移已验证（如适用）
- [ ] Swagger/OpenAPI 已同步（如适用）
- [ ] 正常路径和失败路径已验证

## 风险与依赖

<!-- 写明上下游依赖、兼容性处理、需要联调或仍未关闭的风险；无则写“无”。 -->

## 后端规范自查（后端 PR 必填，前端/文档 PR 可删）

- [ ] 所有接口已标注 `[Authorize]` 及角色/策略；数据范围按"角色 × 部门"实现（主管限本部门、HR/管理员跨部门、员工限本人）
- [ ] 返回统一使用 `ApiResponse`/`PagedResult`（继承 `ApiControllerBase`）；失败路径抛 `BusinessException` 子类，未自建响应类/异常类，Controller 内无 try/catch 状态码
- [ ] Repository 使用 Dapper + `IDbConnectionFactory`；SQL 列名已逐列对照 `database/oracle/migrations/V001__init_schema.sql`；绑定变量未使用 Oracle 保留字（如 `:Comment`）
- [ ] 本地 `dotnet build` 已通过（附输出或截图）；已连真实数据库验证过至少一条成功和一条失败路径

## 合并前检查

- [ ] 未提交密码、Token、连接串、私钥或本地配置
- [ ] 未直接修改其他模块内部逻辑，或已取得其负责人确认
- [ ] 已更新需要同步的文档、枚举、DTO、前端类型和测试
