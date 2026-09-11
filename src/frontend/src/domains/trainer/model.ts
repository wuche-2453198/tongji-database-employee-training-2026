import type { DomainPage, DomainPageQuery, ServiceRequestOptions } from '@/domains/shared'

export interface TrainerQuery extends DomainPageQuery {
  name?: string
  company?: string
  internal?: 'Y' | 'N'
}

export interface Trainer {
  id: string
  name: string
  title: string | null
  company: string | null
  phone: string | null
  email: string | null
  starLevel: number
  isInternal: boolean
}

export interface TrainerInput {
  name: string
  title?: string
  company?: string
  phone?: string
  email?: string
  starLevel: number
  isInternal: boolean
}

export interface TrainerService {
  list(query: TrainerQuery, options?: ServiceRequestOptions): Promise<DomainPage<Trainer>>
  create(input: TrainerInput, options?: ServiceRequestOptions): Promise<Trainer>
  update(id: string, input: TrainerInput, options?: ServiceRequestOptions): Promise<Trainer>
}
