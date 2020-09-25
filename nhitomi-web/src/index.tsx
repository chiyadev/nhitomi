import { startProgress, stopProgress } from "./progress";
import "./index.css";

(async () => {
  startProgress();

  try {
    await import("./bootstrap");
  } finally {
    stopProgress();
  }
})();
