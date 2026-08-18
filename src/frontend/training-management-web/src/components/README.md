# 公共组件

公共组件统一使用 `src/styles/tokens.css` 中的语义令牌，并基于 Element Plus 完成交互和可访问性能力。业务页面不得复制组件内部样式。

## 导入

```ts
import { AppButton, DataTable, PageState, StatusTag } from '@/components/common'
import { CourseSummary, FileEntry } from '@/components/business'
```

## 组件边界

- `AppButton`：主要、默认、文字、危险、禁用和加载状态；禁用原因需要可见说明。
- `StatusTag`：成功、警告、错误、信息和中性语义，始终包含中文文字。
- `PageHeader`：列表/详情标题、面包屑、返回入口和唯一页面主操作。
- `PageState`：加载、空数据、无结果、失败、403 和 404；失败状态可显示 `traceId`，结果未知等不可重试状态使用 `hide-primary` 隐藏默认恢复按钮。
- `SearchPanel`：Enter 查询、重置、展开/收起和查询中状态。
- `DataTable`：正常、局部加载、空和失败状态；空值统一显示 `—`。
- `AppPagination`：服务端分页，默认每页选项为 10、20、50。
- `FormField`、`AppSelect`：可见标签、帮助、错误、禁用和只读语义。
- `ConfirmDialog`：普通/危险确认和提交中保护；取消在左、确认在右。
- `AppDescriptions`：一列/两列只读信息和空值兜底。
- `ProcessTimeline`：完成、当前、未来和异常终止，只用于展示流程。
- `StatCard`：加载、正常、有效空值和可跳转状态。
- `CourseSummary`：展示服务端课程状态、剩余名额与资格结果，不在前端推导。
- `FileEntry`：可用、失效和无权限；无权限状态不得暴露资源名称或 URL。

开发态展示入口为 `/components`。示例数据只用于验证组件状态，不代表正式业务接口或统计口径。
