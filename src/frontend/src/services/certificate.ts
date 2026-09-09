import type { CertificateService } from '@/domains/certificate'

export type { CertificateService } from '@/domains/certificate'

let servicePromise: Promise<CertificateService> | undefined

export const getCertificateService = (): Promise<CertificateService> => {
  servicePromise ??=
    import.meta.env.VITE_USE_MOCK === 'true'
      ? import('@/mocks/certificate').then(({ mockCertificateService }) => mockCertificateService)
      : import('@/services/http/certificate-http').then(
          ({ httpCertificateService }) => httpCertificateService,
        )

  return servicePromise
}
