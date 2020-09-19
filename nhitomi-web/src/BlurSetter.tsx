import { useLayoutEffect, useMemo } from "react";
import { useConfig } from "./ConfigManager";

function css(s: TemplateStringsArray) {
  return s.toString();
}

export const BlurSetter = () => {
  const style = useMemo(() => {
    const element = document.createElement("style");

    element.type = "text/css";
    element.innerText = css`
      .bg-blur.bg-blur {
        /** double specificity to override bg-color opacities */
        -webkit-backdrop-filter: blur(1em);
        -moz-backdrop-filter: blur(1em);
        backdrop-filter: blur(1em);
        --bg-opacity: 80%;
      }
    `;

    return element;
  }, []);

  const [blur] = useConfig("blur");

  useLayoutEffect(() => {
    const head = document.head;

    if (!head.contains(style) && blur) document.head.appendChild(style);
    else if (head.contains(style) && !blur) document.head.removeChild(style);
  }, [blur, style]);

  return null;
};
