import { startProgress, stopProgress } from "./progress";
import "./index.css";

const version = process.env.REACT_APP_VERSION || "Latest";

(async () => {
  startProgress();

  try {
    checkStorage();

    await import("./bootstrap");
  } catch (e) {
    printMessage(`There was a problem while bootstrapping nhitomi. You may be able to fix this by updating your browser.

>>>
>>> ${e.message}
>>>

${e.stack}`);
  } finally {
    stopProgress();
  }
})();

function checkStorage() {
  try {
    localStorage.setItem("_bootstrap", version);
    localStorage.removeItem("_bootstrap");

    sessionStorage.setItem("_bootstrap", version);
    sessionStorage.removeItem("_bootstrap");
  } catch (e) {
    throw Error(
      `nhitomi requires localStorage and sessionStorage but it is not available. Please see https://stackoverflow.com/a/33047477/13160620. ${e.message}`
    );
  }
}

function printMessage(s: string) {
  const element = document.getElementById("root");
  if (!element) return;

  element.style.whiteSpace = "pre";
  element.style.padding = "1em";

  element.innerText = s;
}
