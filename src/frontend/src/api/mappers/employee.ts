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
    email: dto.email ?? null,
    phone: dto.phone ?? null,
    hireDate: dto.hireDate ?? null,
    status: dto.status === 'RESIGNED' ? 'RESIGNED' : 'ACTIVE',
    createdAt: dto.createdAt ?? null,
  }
}

export function mapEmployee(envelope: ApiEnvelopeDto<EmployeeSummaryDto>): EmployeeSummary {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return mapEmployeeSummary(envelope.data)
}

export function mapEmployeePage(
  envelope: ApiEnvelopeDto<PageDto<EmployeeSummaryDto>>,
): PageResult<EmployeeSummary> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapEmployeeSummary) }
}
