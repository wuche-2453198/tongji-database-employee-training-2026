#!/usr/bin/env bash

set -euo pipefail

required_paths=(
  "README.md"
  "document/README.md"
  "document/01-项目管理/项目执行与人员分工.md"
  "document/01-项目管理/第0阶段任务看板.md"
  "document/01-项目管理/决策日志.md"
  "document/01-项目管理/风险台账.md"
  "document/02-技术设计/技术架构与开发规范.md"
  "document/02-技术设计/Oracle数据库设计与DDL.md"
  "document/03-质量交付/测试联调与发布验收.md"
  "src/backend/README.md"
  "src/frontend/README.md"
  "database/oracle/README.md"
  "tests/api/README.md"
  ".github/PULL_REQUEST_TEMPLATE.md"
)

missing=0
for path in "${required_paths[@]}"; do
  if [[ ! -e "$path" ]]; then
    printf 'missing required project asset: %s\n' "$path" >&2
    missing=1
  fi
done

if (( missing )); then
  exit 1
fi

printf 'project structure: ok\n'
