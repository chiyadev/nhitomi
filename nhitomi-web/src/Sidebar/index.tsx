import React, { ReactNode } from "react";
import { useLayout } from "../LayoutManager";
import { Overlay } from "./Overlay";
import { Strip } from "./Strip";

export const Sidebar = ({ children }: { children?: ReactNode }) => {
  const { screen } = useLayout();

  switch (screen) {
    case "sm":
      return <Overlay>{children}</Overlay>;
    case "lg":
      return <Strip>{children}</Strip>;
  }
};
