import { ServiceError, type UiErrorKind } from '@/types/api'

export type BusinessErrorCode =
  | 'COURSE_NOT_FOUND'
  | 'COURSE_FORBIDDEN'
  | 'COURSE_FULL'
  | 'COURSE_CLOSED'
  | 'REQUEST_EXISTS'
  | 'REQUEST_NOT_FOUND'
  | 'REQUEST_FORBIDDEN'
  | 'REQUEST_REASON_REQUIRED'
  | 'REQUEST_REASON_TOO_LONG'
  | 'REQUEST_STATE_CONFLICT'
  | 'REQUEST_EMPLOYEE_INACTIVE'
  | 'REGISTRATION_EXISTS'
  | 'REGISTRATION_NOT_FOUND'
  | 'REGISTRATION_FORBIDDEN'
  | 'REGISTRATION_NOT_ELIGIBLE'
  | 'REGISTRATION_STATE_CONFLICT'
  | 'ATTENDANCE_NOT_ALLOWED'
  | 'CERTIFICATE_NOT_FOUND'
  | 'CERTIFICATE_FORBIDDEN'
  | 'CERTIFICATE_EXISTS'
  | 'TEST_FORBIDDEN'
  | 'TEST_SCORE_INVALID'
  | 'TEST_TYPE_INVALID'
  | 'TEST_TESTED_AT_INVALID'
  | 'TEST_DUPLICATE'
  | 'TEST_REGISTRATION_REQUIRED'
  | 'TEST_POST_NOT_COMPLETED'
  | 'TEST_COURSE_NOT_FOUND'
  | 'TEST_EMPLOYEE_NOT_FOUND'
  | 'BLACKLIST_FORBIDDEN'
  | 'BLACKLIST_REASON_REQUIRED'
  | 'BLACKLIST_REASON_TOO_LONG'
  | 'BLACKLIST_EMPLOYEE_NOT_FOUND'
  | 'BLACKLIST_SELF'
  | 'BLACKLIST_DUPLICATE'
  | 'BLACKLIST_END_DATE_PAST'
  | 'BLACKLIST_DATE_RANGE_INVALID'
  | 'CONCURRENT_UPDATE'
  | 'RESULT_UNKNOWN'
  | 'HTTP_CONTRACT_NOT_FROZEN'

const defaultKind: Record<BusinessErrorCode, UiErrorKind> = {
  COURSE_NOT_FOUND: 'not-found',
  COURSE_FORBIDDEN: 'forbidden',
  COURSE_FULL: 'conflict',
  COURSE_CLOSED: 'conflict',
  REQUEST_EXISTS: 'conflict',
  REQUEST_NOT_FOUND: 'not-found',
  REQUEST_FORBIDDEN: 'forbidden',
  REQUEST_REASON_REQUIRED: 'validation',
  REQUEST_REASON_TOO_LONG: 'validation',
  REQUEST_STATE_CONFLICT: 'conflict',
  REQUEST_EMPLOYEE_INACTIVE: 'forbidden',
  REGISTRATION_EXISTS: 'conflict',
  REGISTRATION_NOT_FOUND: 'not-found',
  REGISTRATION_FORBIDDEN: 'forbidden',
  REGISTRATION_NOT_ELIGIBLE: 'forbidden',
  REGISTRATION_STATE_CONFLICT: 'conflict',
  ATTENDANCE_NOT_ALLOWED: 'conflict',
  CERTIFICATE_NOT_FOUND: 'not-found',
  CERTIFICATE_FORBIDDEN: 'forbidden',
  CERTIFICATE_EXISTS: 'conflict',
  TEST_FORBIDDEN: 'forbidden',
  TEST_SCORE_INVALID: 'validation',
  TEST_TYPE_INVALID: 'validation',
  TEST_TESTED_AT_INVALID: 'validation',
  TEST_DUPLICATE: 'conflict',
  TEST_REGISTRATION_REQUIRED: 'validation',
  TEST_POST_NOT_COMPLETED: 'validation',
  TEST_COURSE_NOT_FOUND: 'not-found',
  TEST_EMPLOYEE_NOT_FOUND: 'not-found',
  BLACKLIST_FORBIDDEN: 'forbidden',
  BLACKLIST_REASON_REQUIRED: 'validation',
  BLACKLIST_REASON_TOO_LONG: 'validation',
  BLACKLIST_EMPLOYEE_NOT_FOUND: 'not-found',
  BLACKLIST_SELF: 'validation',
  BLACKLIST_DUPLICATE: 'conflict',
  BLACKLIST_END_DATE_PAST: 'validation',
  BLACKLIST_DATE_RANGE_INVALID: 'validation',
  CONCURRENT_UPDATE: 'conflict',
  RESULT_UNKNOWN: 'result-unknown',
  HTTP_CONTRACT_NOT_FROZEN: 'server',
}

const defaultMessage: Record<BusinessErrorCode, string> = {
  COURSE_NOT_FOUND: '课程不存在或已被移除。',
  COURSE_FORBIDDEN: '当前账号无权查看该课程范围。',
  COURSE_FULL: '课程名额已满，请刷新课程状态。',
  COURSE_CLOSED: '课程已关闭，暂时不能操作。',
  REQUEST_EXISTS: '你已经有该课程的待处理申请。',
  REQUEST_NOT_FOUND: '培训申请不存在。',
  REQUEST_FORBIDDEN: '当前账号无权查看该培训申请。',
  REQUEST_REASON_REQUIRED: '请填写申请理由。',
  REQUEST_REASON_TOO_LONG: '申请理由不能超过500字。',
  REQUEST_STATE_CONFLICT: '申请状态已发生变化，请刷新后查看最新结果。',
  REQUEST_EMPLOYEE_INACTIVE: '当前员工账号已停用，不能发起培训申请。',
  REGISTRATION_EXISTS: '你已经报名该课程，请查询最新报名状态。',
  REGISTRATION_NOT_FOUND: '报名记录不存在。',
  REGISTRATION_FORBIDDEN: '当前账号无权查看该报名记录。',
  REGISTRATION_NOT_ELIGIBLE: '当前账号暂不满足报名条件。',
  REGISTRATION_STATE_CONFLICT: '报名状态已发生变化，请刷新后查看最新结果。',
  ATTENDANCE_NOT_ALLOWED: '当前状态不允许执行该考勤操作。',
  CERTIFICATE_NOT_FOUND: '证书不存在。',
  CERTIFICATE_FORBIDDEN: '当前账号无权查看该证书。',
  CERTIFICATE_EXISTS: '该报名记录已经生成证书。',
  TEST_FORBIDDEN: '当前账号无权录入或查看测试成绩。',
  TEST_SCORE_INVALID: '测试分数必须在0到100分之间。',
  TEST_TYPE_INVALID: '测试类型只能为PRE或POST。',
  TEST_TESTED_AT_INVALID: '测试时间不能晚于当前时间。',
  TEST_DUPLICATE: '该员工此课程的该类型成绩已存在，不能重复录入。',
  TEST_REGISTRATION_REQUIRED: '该员工此课程没有有效报名，不能录入PRE成绩。',
  TEST_POST_NOT_COMPLETED: '该员工尚未完成培训，不能录入POST成绩。',
  TEST_COURSE_NOT_FOUND: '课程不存在。',
  TEST_EMPLOYEE_NOT_FOUND: '员工不存在。',
  BLACKLIST_FORBIDDEN: '当前账号无权执行黑名单操作。',
  BLACKLIST_REASON_REQUIRED: '黑名单原因不能为空。',
  BLACKLIST_REASON_TOO_LONG: '黑名单原因长度不能超过 500 字符。',
  BLACKLIST_EMPLOYEE_NOT_FOUND: '员工不存在。',
  BLACKLIST_SELF: '不能将自己加入黑名单。',
  BLACKLIST_DUPLICATE: '该员工当前已在黑名单中。',
  BLACKLIST_END_DATE_PAST: '黑名单结束日期必须晚于今天。',
  BLACKLIST_DATE_RANGE_INVALID: '黑名单结束日期不能早于开始日期。',
  CONCURRENT_UPDATE: '数据状态已发生变化，请刷新后重试。',
  RESULT_UNKNOWN: '操作结果未知，请先查询最新状态，避免重复提交。',
  HTTP_CONTRACT_NOT_FROZEN: '服务暂不可用，请稍后重试。',
}

export function domainError(
  code: BusinessErrorCode,
  options: Partial<{ message: string; traceId: string; kind: UiErrorKind }> = {},
): ServiceError {
  return new ServiceError({
    kind: options.kind ?? defaultKind[code],
    code,
    message: options.message ?? defaultMessage[code],
    traceId: options.traceId,
    fieldErrors: [],
    retryable: false,
    resultUnknown: code === 'RESULT_UNKNOWN',
  })
}
