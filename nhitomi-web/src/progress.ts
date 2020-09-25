import { configure, start, done } from "nprogress";
import { AnimationMode } from "./ConfigManager";

import "./Progress.css";

configureProgress("normal");

export function configureProgress(mode: AnimationMode) {
  let easing: string;

  switch (mode) {
    case "normal":
      easing = "ease";
      break;
    case "faster":
      easing = "cubic-bezier(0, 1, 0, 1)";
      break;
    case "none":
      easing = "steps-start";
      break;
  }

  configure({
    template: `
        <div class="bar" role="bar">
          <div class="peg"></div>
        </div>
        <div class="spinner" role="spinner">
          <div class="spinner-icon"></div>
        </div>
      `,
    easing,
  });
}

let endTimeout: number | undefined;
let count = 0;

export function startProgress() {
  clearTimeout(endTimeout);

  if (count++ === 0) {
    start();
  }
}

export function stopProgress() {
  if (--count === 0) {
    endTimeout = window.setTimeout(done, 200);
  }
}
