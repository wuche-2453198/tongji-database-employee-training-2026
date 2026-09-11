import { fromEnvelopeFailure } from '@/api/error'
import type { ApiEnvelopeDto, EmployeeSummaryDto, PageDto } from '@/api/transport'
import type { PageResult } from '@/types/api'
import type { EmployeeSummary } from '@/domains/employee'

export function mapEmployeeSummary(dto: EmployeeSummaryDto): EmployeeSummary {
  return {
    empId: dto.empId,
    empName: dto.empName,
    deptName: dto.deptName,
    position: dto.position || '—',
  }
}

export function mapEmployeePage(
  envelope: ApiEnvelopeDto<PageDto<EmployeeSummaryDto>>,
): PageResult<EmployeeSummary> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapEmployeeSummary) }
}
