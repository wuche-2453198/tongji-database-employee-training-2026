import { domainError } from '@/domains/errors'
import type { TrainingRequestService } from '@/domains/training-request'

/** TODO(API-Q-005/API-Q-013): 申请接口契约冻结后在此实现 DTO 适配。 */
export const httpTrainingRequestService: TrainingRequestService = {
  async create() {
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
  async listDepartment() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async listForHr() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async approve() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async reject() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async file() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
}
