import { init } from "@sentry/node";

init({
  release: `nhitomi@${process.env.NODE_APP_VERSION || "Latest"}`,
  environment: process.env.NODE_ENV,
});
