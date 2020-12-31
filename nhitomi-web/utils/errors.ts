import { CustomError } from "ts-custom-error";
import { ValidationProblem } from "nhitomi-api";
import { Integrations } from "@sentry/tracing";
import * as SentryBrowser from "@sentry/browser";

export function enableSentry() {
  const dsn = process.env.NEXT_PUBLIC_SENTRY_DSN;
  const release = `nhitomi-web@${process.env.NEXT_PUBLIC_VERSION || "Latest"}`;
  const environment = process.env.NODE_ENV;
  const tracesSampleRate = 0.01;

  if (!dsn) {
    return;
  }

  if (typeof window === "undefined") {
    require("@sentry/node").init({
      dsn,
      release,
      environment,
      tracesSampleRate,
    });
  } else {
    SentryBrowser.init({
      dsn,
      release,
      environment,
      tracesSampleRate,
      integrations: [new Integrations.BrowserTracing()],
    });
  }
}

export class ValidationError extends CustomError {
  list: ValidationProblem[];

  constructor(problems: ValidationProblem[]) {
    super(problems.map(({ field, messages }) => `${field}: ${messages.join(" ")}`).join("\n"));

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
