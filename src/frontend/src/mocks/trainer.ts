import type { Trainer, TrainerService } from '@/domains/trainer'
import { createMockError, currentMockScenario, mockWait } from '@/mocks/scenarios'

let records: Trainer[] = [
  { id: '3001', name: '陈老师', title: '高级数据库工程师', company: '技术平台部', phone: null, email: 'chen@example.com', starLevel: 4.8, isInternal: true },
  { id: '3002', name: '周老师', title: '组织发展顾问', company: '人力资源部', phone: null, email: null, starLevel: 4.6, isInternal: true },
  { id: '3003', name: '林老师', title: '高级产品经理', company: '产品部', phone: null, email: null, starLevel: 4.7, isInternal: true },
]
export const mockTrainerService: TrainerService = {
  async list(query, options) { await mockWait(options?.signal); if (currentMockScenario() === 'failure') throw createMockError('failure'); const name = query.name?.trim().toLowerCase(); const company = query.company?.trim().toLowerCase(); const items = records.filter((record) => (!name || record.name.toLowerCase().includes(name)) && (!company || record.company?.toLowerCase().includes(company)) && (!query.internal || (record.isInternal ? 'Y' : 'N') === query.internal)); const start = (query.page - 1) * query.pageSize; return { items: items.slice(start, start + query.pageSize), page: query.page, pageSize: query.pageSize, total: items.length } },
  async create(input, options) { await mockWait(options?.signal); const record: Trainer = { id: String(Math.max(...records.map((item) => Number(item.id)), 3000) + 1), name: input.name.trim(), title: input.title?.trim() || null, company: input.company?.trim() || null, phone: input.phone?.trim() || null, email: input.email?.trim() || null, starLevel: input.starLevel, isInternal: input.isInternal }; records = [record, ...records]; return record },
  async update(id, input, options) { await mockWait(options?.signal); const existing = records.find((item) => item.id === id); if (!existing) throw createMockError('not-found'); const record = { ...existing, name: input.name.trim(), title: input.title?.trim() || null, company: input.company?.trim() || null, phone: input.phone?.trim() || null, email: input.email?.trim() || null, starLevel: input.starLevel, isInternal: input.isInternal }; records = records.map((item) => item.id === id ? record : item); return record },
}
