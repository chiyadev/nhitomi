import React, { memo } from "react";
import { ButtonGroup, Kbd } from "@chakra-ui/react";
import ViewportBoundOn from "../../../assets/Settings/ViewportBoundOn.png";
import ViewportBoundOff from "../../../assets/Settings/ViewportBoundOff.png";
import SectionItem from "../SectionItem";
import { useT } from "../../../locales";
import { useConfig } from "../../../utils/config";
import ImageRadioButton from "./ImageRadioButton";
import { trackEvent } from "../../../utils/umami";

const ViewportBound = () => {
  const t = useT();
  const [value, setValue] = useConfig("bookViewportBound");

  return (
    <SectionItem
      title={
        <span>
          {t("Settings.Reader.ViewportBound.title")} <Kbd>c</Kbd>
        </span>
      }
      description={t("Settings.Reader.ViewportBound.description")}
    >
      <ButtonGroup>
        <ImageRadioButton
          src={ViewportBoundOn}
          isChecked={value}
          onClick={() => {
            setValue(true);
            trackEvent("settings", `viewportBound${true}`);
          }}
        >
          {t("Settings.Reader.ViewportBound.toggle", { value: true })}
        </ImageRadioButton>

        <ImageRadioButton
          src={ViewportBoundOff}
          isChecked={!value}
          onClick={() => {
            setValue(false);
            trackEvent("settings", `viewportBound${false}`);
          }}
        >
          {t("Settings.Reader.ViewportBound.toggle", { value: false })}
        </ImageRadioButton>
      </ButtonGroup>
    </SectionItem>
  );
};

export default memo(ViewportBound);
