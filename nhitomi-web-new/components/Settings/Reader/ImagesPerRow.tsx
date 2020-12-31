import React, { memo } from "react";
import { ButtonGroup, Kbd } from "@chakra-ui/react";
import ImagesPerRow1 from "../../../assets/Settings/ImagesPerRow1.png";
import ImagesPerRow2 from "../../../assets/Settings/ImagesPerRow2.png";
import SectionItem from "../SectionItem";
import { useT } from "../../../locales";
import { useConfig } from "../../../utils/config";
import ImageRadioButton from "./ImageRadioButton";
import { trackEvent } from "../../../utils/umami";

const ImagesPerRow = () => {
  const t = useT();
  const [value, setValue] = useConfig("bookImagesPerRow");

  return (
    <SectionItem
      title={
        <span>
          {t("Settings.Reader.ImagesPerRow.title")} <Kbd>x</Kbd>
        </span>
      }
      description={t("Settings.Reader.ImagesPerRow.description")}
    >
      <ButtonGroup>
        <ImageRadioButton
          src={ImagesPerRow1}
          isChecked={value === 1}
          onClick={() => {
            setValue(1);
            trackEvent("settings", `imagesPerRow${1}`);
          }}
        >
          {t("Settings.Reader.ImagesPerRow.toggle", { value: 1 })}
        </ImageRadioButton>

        <ImageRadioButton
          src={ImagesPerRow2}
          isChecked={value === 2}
          onClick={() => {
            setValue(2);
            trackEvent("settings", `imagesPerRow${2}`);
          }}
        >
          {t("Settings.Reader.ImagesPerRow.toggle", { value: 2 })}
        </ImageRadioButton>
      </ButtonGroup>
    </SectionItem>
  );
};

export default memo(ImagesPerRow);
