import { init } from "@sentry/react";
import { Integrations } from "@sentry/tracing";

init({
  dsn: process.env.REACT_APP_SENTRY_DSN,
  release: `nhitomi@${process.env.REACT_APP_VERSION || "Latest"}`,
  environment: process.env.NODE_ENV,
  integrations: [new Integrations.BrowserTracing()],
  tracesSampleRate: 0.01,
});
