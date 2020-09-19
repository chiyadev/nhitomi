import React, { ComponentProps } from "react";
import { OutboundLink } from "react-ga";

export const Anchor = (
  props: Omit<ComponentProps<typeof OutboundLink>, "ref" | "to" | "eventLabel" | "href"> & {
    href: string;
  }
) => {
  return <OutboundLink eventLabel={props.href} to={props.href} {...props} />;
};
