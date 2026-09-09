import { domainError } from '@/domains/errors'
import type { CertificateService } from '@/domains/certificate'

/** TODO(API-Q-019/API-Q-020): 证书候选、生成和状态字段契约冻结后在此实现 DTO 适配。 */
export const httpCertificateService: CertificateService = {
  async listMine() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async getById() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async generate() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async getActionEligibility() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async listManage() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
  async listCandidates() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
}
