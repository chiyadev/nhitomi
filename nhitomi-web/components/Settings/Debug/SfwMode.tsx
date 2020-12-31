import React, { memo } from "react";
import { useT } from "../../../locales";
import SectionItem from "../SectionItem";
import { useConfig } from "../../../utils/config";
import { HStack, Switch } from "@chakra-ui/react";

const SfwMode = () => {
  const t = useT();
  const [value, setValue] = useConfig("sfw");

  return (
    <SectionItem title={t("Settings.Debug.SfwMode.title")} description={t("Settings.Debug.SfwMode.description")}>
      <HStack as="label" spacing={2}>
        <Switch isChecked={value} onChange={({ currentTarget: { checked } }) => setValue(checked)} />
        <div>{t("Settings.Debug.SfwMode.toggle", { value })}</div>
      </HStack>
    </SectionItem>
  );
};

export default memo(SfwMode);
