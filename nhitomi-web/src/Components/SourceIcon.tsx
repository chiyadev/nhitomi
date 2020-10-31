import React, { ComponentProps } from "react";
import { ScraperType } from "nhitomi-api";

import nhentai from "../assets/icons/nhentai.jpg";
import hitomi from "../assets/icons/hitomi.jpg";

const icons: { [type in Exclude<ScraperType, "unknown">]: string } = {
  nhentai,
  hitomi,
};

export const SourceIcon = ({ type, ...props }: { type: ScraperType } & ComponentProps<"img">) => {
  return <img alt={type} src={type === "unknown" ? undefined : icons[type]} {...props} />;
};
