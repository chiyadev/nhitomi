import { CustomError } from "ts-custom-error";
import { ValidationProblem } from "nhitomi-api";

export type SerializableError = {
  name: string;
  message: string;
  stack?: string;
};

export function makeErrorSerializable(err: Error): SerializableError {
  return {
    name: err.name,
    message: err.message,
    stack: err.stack,
  };
}

export class ValidationError extends CustomError {
  list: ValidationProblem[];

  constructor(message: string, problems: ValidationProblem[]) {
    super(message);

    this.list = problems;
  }

  /** Finds the first validation problem with the given field prefix. */
  find(prefix: string) {
    return this.list.find((p) => this.isPrefixed(p, prefix));
  }

  /** Removes all validation problems beginning with the given prefix. */
  remove(prefix: string) {
    this.list = this.list.filter((p) => !this.isPrefixed(p, prefix));
  }

  private isPrefixed(problem: ValidationProblem, prefix: string) {
    const field = problem.field.split(".");

    for (let i = 0; i < field.length; i++) {
      const part = field.slice(i).join(".");

      if (part.startsWith(prefix)) return true;
    }

    return false;
  }
}
