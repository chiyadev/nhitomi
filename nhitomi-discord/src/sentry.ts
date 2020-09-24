import { init } from "@sentry/node";

init({
  release: `nhitomi-discord@${process.env.NODE_APP_VERSION || "Latest"}`,
  environment: process.env.NODE_ENV,
});
