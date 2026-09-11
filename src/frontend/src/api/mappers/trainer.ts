import { fromEnvelopeFailure } from '@/api/error'
import type { ApiEnvelopeDto, PageDto, TrainerDto } from '@/api/transport'
import type { Trainer } from '@/domains/trainer'
import type { PageResult } from '@/types/api'

export const mapTrainerRecord = (dto: TrainerDto): Trainer => ({
  id: String(dto.trainerId),
  name: dto.trainerName,
  title: dto.title,
  company: dto.company,
  phone: dto.phone,
  email: dto.email,
  starLevel: Number(dto.starLevel),
  isInternal: dto.isInternal === 'Y',
})
export function mapTrainer(envelope: ApiEnvelopeDto<TrainerDto>): Trainer {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return mapTrainerRecord(envelope.data)
}
export function mapTrainerPage(envelope: ApiEnvelopeDto<PageDto<TrainerDto>>): PageResult<Trainer> {
  if (!envelope.success || !envelope.data) throw fromEnvelopeFailure(envelope)
  return { ...envelope.data, items: envelope.data.items.map(mapTrainerRecord) }
}
