import type { DepartmentBudget, DepartmentBudgetService } from '@/domains/budget'
import { createMockError, currentMockScenario, mockWait } from '@/mocks/scenarios'

let records: DepartmentBudget[] = [
  { id: '101', departmentName: '技术部', annualBudget: 200000, usedBudget: 68000, remainingBudget: 132000 },
  { id: '102', departmentName: '人力资源部', annualBudget: 120000, usedBudget: 25000, remainingBudget: 95000 },
  { id: '103', departmentName: '信息管理部', annualBudget: 180000, usedBudget: 75000, remainingBudget: 105000 },
  { id: '104', departmentName: '市场部', annualBudget: 150000, usedBudget: 30000, remainingBudget: 120000 },
]

export const mockDepartmentBudgetService: DepartmentBudgetService = {
  async list(query, options) {
    await mockWait(options?.signal)
    if (currentMockScenario() === 'failure') throw createMockError('failure')
    const keyword = query.keyword?.trim().toLowerCase()
    const matching = records.filter((record) =>
      (!keyword || record.departmentName.toLowerCase().includes(keyword)) &&
      (query.minBudget === undefined || record.annualBudget >= query.minBudget) &&
      (query.maxBudget === undefined || record.annualBudget <= query.maxBudget),
    )
    const start = (query.page - 1) * query.pageSize
    return { items: matching.slice(start, start + query.pageSize), page: query.page, pageSize: query.pageSize, total: matching.length }
  },
  async create(input, options) {
    await mockWait(options?.signal)
    if (records.some((record) => record.departmentName === input.departmentName.trim())) throw createMockError('failure')
    const id = String(Math.max(...records.map((record) => Number(record.id)), 100) + 1)
    const record = { id, departmentName: input.departmentName.trim(), annualBudget: input.annualBudget, usedBudget: 0, remainingBudget: input.annualBudget }
    records = [record, ...records]
    return record
  },
  async update(id, input, options) {
    await mockWait(options?.signal)
    const index = records.findIndex((record) => record.id === id)
    if (index < 0) throw createMockError('not-found')
    const existing = records[index]!
    if (input.annualBudget < existing.usedBudget) throw createMockError('failure')
    const record = { ...existing, departmentName: input.departmentName.trim(), annualBudget: input.annualBudget, remainingBudget: input.annualBudget - existing.usedBudget }
    records = records.map((item) => item.id === id ? record : item)
    return record
  },
  async delete(id, options) {
    await mockWait(options?.signal)
    if (!records.some((record) => record.id === id)) throw createMockError('not-found')
    records = records.filter((record) => record.id !== id)
  },
}
