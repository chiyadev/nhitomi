import React, { ComponentProps, memo } from "react";
import { LanguageType } from "nhitomi-api";
import { chakra } from "@chakra-ui/react";
import Flag from "react-world-flags";

const FlagCore = chakra(Flag);

const LanguageFlag = ({ language, ...props }: { language: LanguageType } & ComponentProps<typeof FlagCore>) => {
  return <FlagCore code={language.split("-")[1]} w="1.5em" h="1em" objectFit="cover" borderRadius="sm" {...props} />;
};

export default memo(LanguageFlag);
