import { domainError } from '@/domains/errors'
import type { TestService } from '@/domains/test'

/** TODO(API-Q-022): 培训测试契约冻结后在此实现 DTO 适配。 */
export const httpTestService: TestService = {
  async listManage() {
    throw domainError('HTTP_CONTRACT_NOT_FROZEN')
  },
}
