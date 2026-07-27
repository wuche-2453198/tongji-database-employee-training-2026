# API 测试入口

维护协调人：彭浩。所有主路径和关键失败路径使用 `.http` 文件或导出的 Postman/Apifox 集合保存，保证他人可复现。

建议首批文件：

```text
auth.http
health.http
employees.http
courses.http
training-requests.http
registrations.http
results.http
demo-flow.http
```

每个用例注明所需角色、前置数据、请求、预期状态码和关键响应字段。主流程和最小失败矩阵见 [测试联调与发布验收](../../document/03-质量交付/测试联调与发布验收.md)。
