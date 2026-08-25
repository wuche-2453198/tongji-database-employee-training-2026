import { requestApi } from '@/api/client'
import { mapRegistrationReceipt } from '@/api/mappers/course'
import type { ApiEnvelopeDto, RegistrationReceiptDto } from '@/api/transport'
import { domainError } from '@/domains/errors'
import type { RegistrationService } from '@/domains/registration'

export const httpRegistrationService: RegistrationService = {
  async create(command, options) {
    // TODO(API-Q-011/API-Q-022): requestId、幂等键和最终报名 DTO 待后端冻结。
    const response = await requestApi<ApiEnvelopeDto<RegistrationReceiptDto>, { courseId: string }>(
      {
        method: 'POST',
        url: '/api/registrations',
        data: command,
        signal: options?.signal,
        operation: 'write',
      },
    )
    const receipt = mapRegistrationReceipt(response)
    return {
      id: receipt.registrationId,
      courseId: receipt.courseId,
      courseName: '—',
      employeeId: 0,
      employeeName: '—',
      status: 'REGISTERED',
      registeredAt: receipt.registeredAt,
      signedInAt: null,
      completedAt: null,
    }
  },
  async getMineByCourse() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async listMine() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async getById() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async getActionEligibility() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async listManage() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async cancel() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async signIn() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async markAbsent() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async complete() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
}
