export class LatestRequestController {
  private controller?: AbortController

  async run<T>(task: (signal: AbortSignal) => Promise<T>): Promise<T> {
    this.controller?.abort()
    const controller = new AbortController()
    this.controller = controller

    try {
      return await task(controller.signal)
    } finally {
      if (this.controller === controller) this.controller = undefined
    }
  }

  cancel(): void {
    this.controller?.abort()
    this.controller = undefined
  }
}

export class SingleFlightController {
  private readonly requests = new Map<string, Promise<unknown>>()

  run<T>(key: string, task: () => Promise<T>): Promise<T> {
    const current = this.requests.get(key) as Promise<T> | undefined
    if (current) return current

    const request = task().finally(() => this.requests.delete(key))
    this.requests.set(key, request)
    return request
  }

  isRunning(key: string): boolean {
    return this.requests.has(key)
  }
}
