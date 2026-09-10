import { requestApi } from '@/api/client'
import { fromEnvelopeFailure } from '@/api/error'
import {
  mapAttendance,
  mapRegistration,
  mapRegistrationPage,
  mapRegistrationSummary,
} from '@/api/mappers/registration'
import type {
  ApiEnvelopeDto,
  AttendanceDto,
  PageDto,
  RegistrationDto,
  RegistrationSummaryDto,
} from '@/api/transport'
import type { Registration, RegistrationQuery, RegistrationService } from '@/domains/registration'

const unwrap = <T>(envelope: ApiEnvelopeDto<T>): T => {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return envelope.data
}

const queryParams = (query: RegistrationQuery) => ({
  CourseId: query.courseId ? Number(query.courseId) : undefined,
  EmpName: query.employeeKeyword || undefined,
  CourseName: query.keyword || undefined,
  Status: query.status === 'UNKNOWN' ? undefined : query.status || undefined,
  StartAtFrom: query.startDateFrom || undefined,
  StartAtTo: query.startDateTo || undefined,
  SortBy: query.sortBy || undefined,
  SortDirection: query.sortDirection || undefined,
  Page: query.page,
  PageSize: query.pageSize,
})

const getRegistration = async (id: string, signal?: AbortSignal): Promise<Registration> => {
  const envelope = await requestApi<ApiEnvelopeDto<RegistrationDto>>({
    method: 'GET',
    url: `/api/registrations/${encodeURIComponent(id)}`,
    signal,
    operation: 'query',
  })
  return mapRegistration(unwrap(envelope))
}

export const httpRegistrationService: RegistrationService = {
  async create(command, options) {
    const envelope = await requestApi<ApiEnvelopeDto<RegistrationDto>>({
      method: 'POST',
      url: '/api/registrations',
      data: { CourseId: Number(command.courseId) },
      signal: options?.signal,
      operation: 'write',
    })
    return mapRegistration(unwrap(envelope))
  },

  async getMineByCourse(courseId, options) {
    const page = await this.listMine({ courseId, page: 1, pageSize: 1 }, options)
    return page.items.find((item) => item.status !== 'CANCELED') ?? null
  },

  async listMine(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<PageDto<RegistrationDto>>>({
      method: 'GET',
      url: '/api/registrations/my',
      params: queryParams(query),
      signal: options?.signal,
      operation: 'query',
    })
    return mapRegistrationPage(envelope)
  },

  async getById(id, options) {
    return getRegistration(id, options?.signal)
  },

  async listManage(query, options) {
    const envelope = await requestApi<ApiEnvelopeDto<PageDto<RegistrationDto>>>({
      method: 'GET',
      url: '/api/registrations',
      params: queryParams(query),
      signal: options?.signal,
      operation: 'query',
    })
    return mapRegistrationPage(envelope)
  },

  async summary(courseId, options) {
    const envelope = await requestApi<ApiEnvelopeDto<RegistrationSummaryDto>>({
      method: 'GET',
      url: '/api/registrations/summary',
      params: { CourseId: courseId ? Number(courseId) : undefined },
      signal: options?.signal,
      operation: 'query',
    })
    return mapRegistrationSummary(envelope)
  },

  async cancel(id, options) {
    const envelope = await requestApi<ApiEnvelopeDto<RegistrationDto>>({
      method: 'PATCH',
      url: `/api/registrations/${encodeURIComponent(id)}/cancel`,
      data: {},
      signal: options?.signal,
      operation: 'write',
    })
    return mapRegistration(unwrap(envelope))
  },

  async signIn(id, options) {
    const envelope = await requestApi<ApiEnvelopeDto<AttendanceDto>>({
      method: 'POST',
      url: `/api/registrations/${encodeURIComponent(id)}/signin`,
      signal: options?.signal,
      operation: 'write',
    })
    return mapAttendance(unwrap(envelope))
  },

  async manualSignIn(command, options) {
    const envelope = await requestApi<ApiEnvelopeDto<AttendanceDto>>({
      method: 'POST',
      url: '/api/attendance/manual',
      data: {
        RegId: Number(command.regId),
        SigninTime: command.signinTime || undefined,
        Remark: command.remark,
      },
      signal: options?.signal,
      operation: 'write',
    })
    return mapAttendance(unwrap(envelope))
  },

  async markAbsent(id, options) {
    const envelope = await requestApi<ApiEnvelopeDto<RegistrationDto>>({
      method: 'PATCH',
      url: `/api/registrations/${encodeURIComponent(id)}/absent`,
      data: {},
      signal: options?.signal,
      operation: 'write',
    })
    return mapRegistration(unwrap(envelope))
  },

  async complete(id, options) {
    const envelope = await requestApi<ApiEnvelopeDto<RegistrationDto>>({
      method: 'PATCH',
      url: `/api/registrations/${encodeURIComponent(id)}/complete`,
      data: {},
      signal: options?.signal,
      operation: 'write',
    })
    return mapRegistration(unwrap(envelope))
  },
}
