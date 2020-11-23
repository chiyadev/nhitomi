import React, { createContext, memo, ReactNode } from "react";
import { parseCookies } from "nookies";
import { useRouter } from "next/router";
import { RawConfig } from "../utils/config";

export const RawConfigContext = createContext<RawConfig>({});

const ConfigProvider = ({ config, children }: { config: RawConfig; children?: ReactNode }) => {
  const router = useRouter();

  if (typeof window !== "undefined") {
    config = {
      ...parseCookies(),
      ...router.query,
    };
  }

  return <RawConfigContext.Provider value={config}>{children}</RawConfigContext.Provider>;
};

export default memo(ConfigProvider);
