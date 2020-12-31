import React, { memo } from "react";
import { ButtonGroup, Kbd } from "@chakra-ui/react";
import LeftToRightOn from "../../../assets/Settings/LeftToRightOn.png";
import LeftToRightOff from "../../../assets/Settings/LeftToRightOff.png";
import SectionItem from "../SectionItem";
import { useT } from "../../../locales";
import { useConfig } from "../../../utils/config";
import ImageRadioButton from "./ImageRadioButton";
import { trackEvent } from "../../../utils/umami";

const LeftToRight = () => {
  const t = useT();
  const [value, setValue] = useConfig("bookLeftToRight");
  const [imagesPerRow] = useConfig("bookImagesPerRow");

  return (
    <SectionItem
      title={
        <span>
          {t("Settings.Reader.LeftToRight.title")} <Kbd>l</Kbd>
        </span>
      }
      description={t("Settings.Reader.LeftToRight.description")}
    >
      <ButtonGroup>
        <ImageRadioButton
          src={LeftToRightOn}
          isChecked={imagesPerRow === 2 && value}
          onClick={() => {
            setValue(true);
            trackEvent("settings", `leftToRight${true}`);
          }}
          disabled={imagesPerRow !== 2}
        >
          {t("Settings.Reader.LeftToRight.toggle", { value: true })}
        </ImageRadioButton>

        <ImageRadioButton
          src={LeftToRightOff}
          isChecked={imagesPerRow === 2 && !value}
          onClick={() => {
            setValue(false);
            trackEvent("settings", `leftToRight${false}`);
          }}
          disabled={imagesPerRow !== 2}
        >
          {t("Settings.Reader.LeftToRight.toggle", { value: false })}
        </ImageRadioButton>
      </ButtonGroup>
    </SectionItem>
  );
};

export default memo(LeftToRight);
