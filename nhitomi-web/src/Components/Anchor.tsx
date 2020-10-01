import React, { ComponentProps } from "react";
import { trackEvent } from "../track";

export const Anchor = ({ onClick, href, ...props }: ComponentProps<"a">) => {
  return (
    <a
      onClick={(event) => {
        if (href) {
          const url = new URL(href, new URL(window.location.href));

          trackEvent("link", url.host);
        }

        return onClick?.(event);
      }}
      href={href}
      {...props}
    />
  );
};
