import { ReactionTrigger } from '../interactive'

export type ListTriggerTarget = {
  position: number
}

export class ListTrigger extends ReactionTrigger {
  get emoji(): string {
    switch (this.direction) {
      case 'left': return '\u25c0'
      case 'right': return '\u25b6'
      default: return ''
    }
  }

  constructor(
    readonly target: ListTriggerTarget,
    readonly direction: 'left' | 'right'
  ) {
    super()
  }

  protected async run(): Promise<boolean> {
    switch (this.direction) {
      case 'left': --this.target.position; break
      case 'right': ++this.target.position; break
    }

    return true
  }
}

export type ListJumpTriggerTarget = ListTriggerTarget & {
  end: number
}

export class ListJumpTrigger extends ReactionTrigger {
  get emoji(): string {
    switch (this.direction) {
      case 'start': return '\u23EA'
      case 'end': return '\u23E9'
      default: return ''
    }
  }

  constructor(
    readonly target: ListJumpTriggerTarget,
    readonly direction: 'start' | 'end'
  ) {
    super()
  }

  protected async run(): Promise<boolean> {
    switch (this.direction) {
      case 'start': this.target.position = 0; break
      case 'end': this.target.position = this.target.end; break
    }

    return true
  }
}
