import { requestApi } from '@/api/client'
import { fromEnvelopeFailure } from '@/api/error'
import { mapCertificate, mapCertificateCandidate } from '@/api/mappers/certificate'
import type {
  ApiEnvelopeDto,
  CertificateCandidateDto,
  CertificateDto,
  PageDto,
} from '@/api/transport'
import { domainError } from '@/domains/errors'
import type {
  Certificate,
  CertificateCandidate,
  CertificateQuery,
  CertificateService,
} from '@/domains/certificate'
import type { DomainPage } from '@/domains/shared'

interface GenerateCertificateResultDto {
  certificateId: number
  certificateCode: string
}

const unwrap = <T>(envelope: ApiEnvelopeDto<T>): T => {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return envelope.data
}

const getCertificate = async (id: string, signal?: AbortSignal): Promise<Certificate> => {
  const envelope = await requestApi<ApiEnvelopeDto<CertificateDto>>({
    method: 'GET',
    url: `/api/certificates/${encodeURIComponent(id)}`,
    signal,
    operation: 'query',
  })
  return mapCertificate(unwrap(envelope))
}

const pageOf = (items: Certificate[], query: CertificateQuery): DomainPage<Certificate> => {
  const page = Number.isInteger(query.page) && query.page > 0 ? query.page : 1
  const pageSize = query.pageSize > 0 ? query.pageSize : 20
  const start = (page - 1) * pageSize
  return { items: items.slice(start, start + pageSize), page, pageSize, total: items.length }
}

const manageQueryParams = (query: CertificateQuery) => ({
  EmployeeName: query.employeeKeyword || undefined,
  CourseName: query.keyword || undefined,
  StartDateFrom: query.startDateFrom || undefined,
  StartDateTo: query.startDateTo || undefined,
  Page: query.page,
  PageSize: query.pageSize,
})

export const httpCertificateService: CertificateService = {
  async listMine(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<CertificateDto[]>>({
      method: 'GET',
      url: '/api/certificates/my',
      signal: options?.signal,
      operation: 'query',
    })
    const items = unwrap(envelope).map(mapCertificate)

    const keyword = query.keyword?.trim().toLowerCase()
    const status = query.status
    const filtered = items
      .filter((item) => (keyword ? item.courseName.toLowerCase().includes(keyword) : true))
      .filter((item) => (status ? item.displayStatus === status : true))
      .sort((left, right) => right.issuedAt.localeCompare(left.issuedAt))

    return pageOf(filtered, query)
  },

  async getById(id, options) {
    return getCertificate(id, options?.signal)
  },

  async generate(command, options) {
    const envelope = await requestApi<ApiEnvelopeDto<GenerateCertificateResultDto>>({
      method: 'POST',
      url: '/api/certificates',
      data: { RegistrationId: Number(command.registrationId) },
      signal: options?.signal,
      operation: 'write',
    })
    const result = unwrap(envelope)
    if (!result.certificateId) throw domainError('RESULT_UNKNOWN')
    return getCertificate(String(result.certificateId), options?.signal)
  },

  async listManage(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<PageDto<CertificateDto>>>({
      method: 'GET',
      url: '/api/certificates',
      params: manageQueryParams(query),
      signal: options?.signal,
      operation: 'query',
    })
    if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
    return { ...envelope.data, items: envelope.data.items.map(mapCertificate) }
  },

  async listCandidates(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<PageDto<CertificateCandidateDto>>>({
      method: 'GET',
      url: '/api/certificates/candidates',
      params: manageQueryParams(query),
      signal: options?.signal,
      operation: 'query',
    })
    if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
    const items: CertificateCandidate[] = envelope.data.items.map(mapCertificateCandidate)
    return { ...envelope.data, items }
  },

  // 后端暂无资格查询接口,保留契约占位,待组织模块提供后实现。
  async getActionEligibility() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
}
