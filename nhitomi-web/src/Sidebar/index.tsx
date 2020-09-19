import React, { ReactNode } from "react";
import { useLayout } from "../LayoutManager";
import { Overlay } from "./Overlay";
import { Strip } from "./Strip";

export const Sidebar = ({ children }: { children?: ReactNode }) => {
  const { screen } = useLayout();

  switch (screen) {
    case "sm":
      return <Overlay children={children} />;
    case "lg":
      return <Strip children={children} />;
  }
};
