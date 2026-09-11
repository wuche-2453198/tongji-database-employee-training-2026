import type {
  Certificate,
  CertificateCandidate,
  CertificateQuery,
  CertificateService,
} from '@/domains/certificate'
import { domainError } from '@/domains/errors'
import { getMockActor, mockBusinessRepository } from '@/mocks/repositories/business-repository'
import { createMockError, currentMockScenario, mockWait } from '@/mocks/scenarios'

const PAGE_SIZES = [10, 20, 50]

function canManage(): boolean {
  const role = getMockActor().role
  return role === 'HR' || role === 'ADMIN'
}

function canRead(item: Certificate): boolean {
  const actor = getMockActor()
  return canManage() || (actor.role === 'EMPLOYEE' && item.employeeId === actor.employeeId)
}

function pageOf<T>(items: T[], query: CertificateQuery) {
  const page = Number.isInteger(query.page) && query.page > 0 ? query.page : 1
  const pageSize = PAGE_SIZES.includes(query.pageSize) ? query.pageSize : 20
  const start = (page - 1) * pageSize
  return { items: items.slice(start, start + pageSize), page, pageSize, total: items.length }
}

function filterCertificates(query: CertificateQuery, manage: boolean): Certificate[] {
  const actor = getMockActor()
  let items = mockBusinessRepository
    .getState()
    .certificates.filter((item) => (manage ? true : item.employeeId === actor.employeeId))
  const keyword = query.keyword?.trim().toLowerCase()
  const employeeKeyword = query.employeeKeyword?.trim().toLowerCase()
  if (keyword) items = items.filter((item) => item.courseName.toLowerCase().includes(keyword))
  if (employeeKeyword)
    items = items.filter(
      (item) =>
        item.employeeName.toLowerCase().includes(employeeKeyword) ||
        String(item.employeeId).includes(employeeKeyword),
    )
  if (query.departmentName && query.departmentName !== '技术部') items = []
  if (query.status) items = items.filter((item) => item.displayStatus === query.status)
  if (query.startDateFrom)
    items = items.filter((item) => item.issuedAt.slice(0, 10) >= query.startDateFrom!)
  if (query.startDateTo)
    items = items.filter((item) => item.issuedAt.slice(0, 10) <= query.startDateTo!)
  return items.sort((left, right) => right.issuedAt.localeCompare(left.issuedAt))
}

function candidateItems(query: CertificateQuery): CertificateCandidate[] {
  const state = mockBusinessRepository.getState()
  const existing = new Set(state.certificates.map((item) => `${item.courseId}:${item.employeeId}`))
  let items = state.registrations
    .filter((item) => item.status === 'COMPLETED')
    .filter((item) => !existing.has(`${item.courseId}:${item.employeeId}`))
    .map((item) => ({
      registrationId: item.id,
      courseId: item.courseId,
      courseName: item.courseName,
      employeeId: item.employeeId,
      employeeName: item.employeeName,
      departmentName: item.departmentName || '—',
      registrationStatus: item.status,
      actualHours: item.actualHours ?? null,
      qualification: { allowed: true },
    }))
  const keyword = query.keyword?.trim().toLowerCase()
  const employeeKeyword = query.employeeKeyword?.trim().toLowerCase()
  if (keyword) items = items.filter((item) => item.courseName.toLowerCase().includes(keyword))
  if (employeeKeyword)
    items = items.filter(
      (item) =>
        item.employeeName.toLowerCase().includes(employeeKeyword) ||
        String(item.employeeId).includes(employeeKeyword),
    )
  if (query.departmentName)
    items = items.filter((item) => item.departmentName === query.departmentName)
  return items
}

export const mockCertificateService: CertificateService = {
  async listMine(query, options) {
    await mockWait(options?.signal, 50)
    if (currentMockScenario() === 'failure') throw createMockError('failure')
    return pageOf(filterCertificates(query, false), query)
  },

  async getById(id, options) {
    await mockWait(options?.signal, 50)
    const item = mockBusinessRepository
      .getState()
      .certificates.find((certificate) => certificate.id === id)
    if (!item) throw domainError('CERTIFICATE_NOT_FOUND')
    if (!canRead(item)) throw domainError('CERTIFICATE_FORBIDDEN')
    return item
  },

  async generate(command, options) {
    await mockWait(options?.signal)
    if (!canManage()) throw domainError('CERTIFICATE_FORBIDDEN')
    const state = mockBusinessRepository.getState()
    const registration = state.registrations.find((item) => item.id === command.registrationId)
    if (!registration || registration.status !== 'COMPLETED')
      throw domainError('REGISTRATION_NOT_ELIGIBLE', { message: '当前报名记录尚未满足发证条件。' })
    const existing = state.certificates.find(
      (item) =>
        item.courseId === registration.courseId && item.employeeId === registration.employeeId,
    )
    if (existing) throw domainError('CERTIFICATE_EXISTS')
    const certificate: Certificate = {
      id: `1000${state.certificates.length + 2}`,
      certificateNo: 'CERT-2026-MOCK',
      courseId: registration.courseId,
      courseName: registration.courseName,
      employeeId: registration.employeeId,
      employeeName: registration.employeeName,
      issuedAt: '2026-08-23T10:00:00+08:00',
      expiresAt: '2028-08-23T23:59:59+08:00',
      displayStatus: 'VALID',
    }
    mockBusinessRepository.update((next) => next.certificates.push(certificate))
    if (currentMockScenario() === 'result-unknown') throw createMockError('result-unknown')
    return certificate
  },

  async getActionEligibility(id, options) {
    await mockWait(options?.signal, 30)
    const item = mockBusinessRepository.getState().registrations.find((entry) => entry.id === id)
    if (!item) throw domainError('REGISTRATION_NOT_FOUND')
    const existing = mockBusinessRepository
      .getState()
      .certificates.some(
        (certificate) =>
          certificate.courseId === item.courseId && certificate.employeeId === item.employeeId,
      )
    return {
      action: 'generate',
      allowed: canManage() && item.status === 'COMPLETED' && !existing,
      reason: existing
        ? '该报名记录已经生成证书。'
        : item.status !== 'COMPLETED'
          ? '需完成培训后才能生成证书。'
          : undefined,
    }
  },

  async listManage(query, options) {
    await mockWait(options?.signal, 50)
    if (!canManage()) throw domainError('CERTIFICATE_FORBIDDEN')
    if (currentMockScenario() === 'failure') throw createMockError('failure')
    return pageOf(filterCertificates(query, true), query)
  },

  async listCandidates(query, options) {
    await mockWait(options?.signal, 50)
    if (!canManage()) throw domainError('CERTIFICATE_FORBIDDEN')
    const items = currentMockScenario() === 'empty' ? [] : candidateItems(query)
    return pageOf(items, query)
  },
}
