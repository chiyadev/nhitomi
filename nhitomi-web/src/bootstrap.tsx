import "react-app-polyfill/stable";
import "preact/debug";

import "./sentry";
import "./umami";
import "./cacheBuster";

import "./theme";
import "./theme.css";

import React, { StrictMode } from "react";
import { render } from "react-dom";
import { App } from "./App";
import { withProfiler } from "@sentry/react";

const Root = withProfiler(App);

render(
  <StrictMode>
    <Root />
  </StrictMode>,
  document.getElementById("root")
);
