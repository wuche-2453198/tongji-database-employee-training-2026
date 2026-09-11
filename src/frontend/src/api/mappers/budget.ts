import { fromEnvelopeFailure } from '@/api/error'
import type { ApiEnvelopeDto, DepartmentBudgetDto, PageDto } from '@/api/transport'
import type { DepartmentBudget } from '@/domains/budget'
import type { PageResult } from '@/types/api'

export const mapDepartmentBudgetRecord = (dto: DepartmentBudgetDto): DepartmentBudget => ({
  id: String(dto.deptId),
  departmentName: dto.deptName,
  annualBudget: Number(dto.annualBudget),
  usedBudget: Number(dto.usedBudget),
  remainingBudget: Number(dto.remainBudget),
})

export function mapDepartmentBudget(
  envelope: ApiEnvelopeDto<DepartmentBudgetDto>,
): DepartmentBudget {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return mapDepartmentBudgetRecord(envelope.data)
}

export function mapDepartmentBudgetPage(
  envelope: ApiEnvelopeDto<PageDto<DepartmentBudgetDto>>,
): PageResult<DepartmentBudget> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapDepartmentBudgetRecord) }
}
