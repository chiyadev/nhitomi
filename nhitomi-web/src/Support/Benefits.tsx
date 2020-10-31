import React, { ReactNode } from "react";
import { css, cx } from "emotion";
import { useLayout } from "../LayoutManager";
import { getColor } from "../theme";
import { FolderFilled, ThunderboltFilled } from "@ant-design/icons";
import { PinkLabel } from "./PinkLabel";

import downloadsLimitless from "../assets/images/downloads_limitless.jpg";
import downloadsCollection from "../assets/images/downloads_collection.jpg";

export const Benefits = () => {
  return (
    <div className="text-center space-y-8">
      <PinkLabel>Benefits for supporters</PinkLabel>

      <DownloadLimitless />
      <DownloadCollection />
    </div>
  );
};

const GradientImage = ({ children }: { children?: ReactNode }) => (
  <div className="relative pointer-events-none select-none">
    {children}

    <div
      className={cx(
        "absolute w-full h-full bottom-0",
        css`
          background: linear-gradient(to bottom, ${getColor("black").opacity(0).rgb}, ${getColor("black").rgb});
        `
      )}
    />
  </div>
);

const DownloadLimitless = () => {
  const { screen } = useLayout();

  return (
    <div className="space-y-4">
      <div className="text-lg">
        <ThunderboltFilled /> Supercharged downloads
      </div>

      <div>Download multiple books without bandwidth limits.</div>

      <GradientImage>
        <img
          alt="downloads_limitless"
          src={downloadsLimitless}
          className={cx("w-full h-64", {
            "object-contain": screen !== "sm",
            "object-left object-cover": screen === "sm",
          })}
        />
      </GradientImage>
    </div>
  );
};

const DownloadCollection = () => {
  return (
    <div className="space-y-4">
      <div className="text-lg">
        <FolderFilled /> Collection downloads
      </div>

      <div>Bulk download all collection items in a single click.</div>

      <GradientImage>
        <img alt="downloads_collection" src={downloadsCollection} className="w-full h-40 object-contain" />
      </GradientImage>
    </div>
  );
};
