import "preact/debug";
import React from "react";
import { init as sentryInit, withProfiler } from "@sentry/react";
import { Integrations } from "@sentry/tracing";
import { render } from "react-dom";
import { App } from "./App";

sentryInit({
  dsn: "https://a768b944b93b4c70be91bf4df488c978@o452268.ingest.sentry.io/5439405",
  integrations: [new Integrations.BrowserTracing()],
  tracesSampleRate: 0.01,
});

import "./theme.css";
import "./theme";
import "./index.css";

const ProfiledApp = withProfiler(App);

render(
  <React.StrictMode>
    <ProfiledApp />
  </React.StrictMode>,
  document.getElementById("root")
);
