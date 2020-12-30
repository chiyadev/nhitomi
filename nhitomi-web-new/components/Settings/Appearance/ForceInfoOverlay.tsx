import React, { memo } from "react";
import { useConfig } from "../../../utils/config";
import SectionItem from "../SectionItem";
import { useT } from "../../../locales";
import { HStack, Switch } from "@chakra-ui/react";
import { trackEvent } from "../../../utils/umami";

const ForceInfoOverlay = () => {
  const t = useT();
  const [value, setValue] = useConfig("bookForceInfoOverlay");

  return (
    <SectionItem
      title={t("Settings.Appearance.ForceInfoOverlay.title")}
      description={t("Settings.Appearance.ForceInfoOverlay.description")}
    >
      <HStack as="label" spacing={2}>
        <Switch
          isChecked={value}
          onChange={({ currentTarget: { checked } }) => {
            setValue(checked);
            trackEvent("settings", `forceInfoOverlay${value}`);
          }}
        />
        <div>{t("Settings.Appearance.ForceInfoOverlay.toggle", { value })}</div>
      </HStack>
    </SectionItem>
  );
};

export default memo(ForceInfoOverlay);
