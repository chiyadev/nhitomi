import { init } from "@sentry/react";
import { Integrations } from "@sentry/tracing";

init({
  dsn: process.env.REACT_APP_SENTRY_DSN,
  integrations: [new Integrations.BrowserTracing()],
  tracesSampleRate: 0.01,
});
