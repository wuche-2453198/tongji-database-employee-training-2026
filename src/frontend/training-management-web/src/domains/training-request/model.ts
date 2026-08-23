import type {
  ActionEligibility,
  DomainPage,
  DomainPageQuery,
  ServiceRequestOptions,
} from '@/domains/shared'

export type TrainingRequestStatus =
  'PENDING' | 'DEPT_APPROVED' | 'DEPT_REJECTED' | 'HR_FILED' | 'UNKNOWN'

export interface TrainingRequest {
  id: string
  courseId: string
  courseName: string
  employeeId: number
  employeeName: string
  departmentName: string
  reason: string
  status: TrainingRequestStatus
  submittedAt: string
  updatedAt: string | null
  departmentOpinion?: string | null
  hrOpinion?: string | null
}

export interface TrainingRequestQuery extends DomainPageQuery {
  keyword?: string
  employeeKeyword?: string
  departmentName?: string
  status?: TrainingRequestStatus
  startDateFrom?: string
  startDateTo?: string
}

export interface CreateTrainingRequestCommand {
  courseId: string
  reason: string
}

export interface TrainingRequestActionEligibility extends ActionEligibility {
  action: 'create' | 'approve' | 'reject' | 'file'
}

export interface TrainingRequestService {
  create(
    command: CreateTrainingRequestCommand,
    options?: ServiceRequestOptions,
  ): Promise<TrainingRequest>
  listMine(
    query: TrainingRequestQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<TrainingRequest>>
  getById(id: string, options?: ServiceRequestOptions): Promise<TrainingRequest>
  getActionEligibility(
    id: string,
    options?: ServiceRequestOptions,
  ): Promise<TrainingRequestActionEligibility[]>
  listDepartment(
    query: TrainingRequestQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<TrainingRequest>>
  listForHr(
    query: TrainingRequestQuery,
    options?: ServiceRequestOptions,
  ): Promise<DomainPage<TrainingRequest>>
  approve(id: string, opinion: string, options?: ServiceRequestOptions): Promise<TrainingRequest>
  reject(id: string, opinion: string, options?: ServiceRequestOptions): Promise<TrainingRequest>
  file(id: string, opinion: string, options?: ServiceRequestOptions): Promise<TrainingRequest>
}
