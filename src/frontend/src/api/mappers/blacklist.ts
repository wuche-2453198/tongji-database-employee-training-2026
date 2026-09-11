import { fromEnvelopeFailure } from '@/api/error'
import type { ApiEnvelopeDto, BlacklistCandidateDto, BlacklistDto, PageDto } from '@/api/transport'
import type { PageResult } from '@/types/api'
import type { BlacklistCandidate, BlacklistRecord, BlacklistStatus } from '@/domains/blacklist'
import { BLACKLIST_STATUS_LABELS } from '@/domains/blacklist'

const mapStatus = (value: string): BlacklistStatus =>
  value === 'ACTIVE' || value === 'RELEASED' ? value : 'UNKNOWN'

export function mapBlacklistRecord(dto: BlacklistDto): BlacklistRecord {
  const status = mapStatus(dto.status)
  return {
    id: String(dto.blackId),
    empId: dto.empId,
    empName: dto.empName,
    deptName: dto.deptName,
    reason: dto.reason,
    startDate: dto.startDate,
    endDate: dto.endDate,
    status,
    statusLabel: BLACKLIST_STATUS_LABELS[status],
    createdAt: dto.createdAt,
  }
}

export function mapBlacklistCandidate(dto: BlacklistCandidateDto): BlacklistCandidate {
  return {
    empId: dto.empId,
    empName: dto.empName,
    deptName: dto.deptName,
    position: dto.position,
    status: dto.status,
  }
}

export function mapBlacklistCandidatePage(
  envelope: ApiEnvelopeDto<PageDto<BlacklistCandidateDto>>,
): PageResult<BlacklistCandidate> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapBlacklistCandidate) }
}

export function mapBlacklist(envelope: ApiEnvelopeDto<BlacklistDto>): BlacklistRecord {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return mapBlacklistRecord(envelope.data)
}

export function mapBlacklistPage(
  envelope: ApiEnvelopeDto<PageDto<BlacklistDto>>,
): PageResult<BlacklistRecord> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapBlacklistRecord) }
}
