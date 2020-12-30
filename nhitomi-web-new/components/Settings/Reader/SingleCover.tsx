import React, { memo } from "react";
import { ButtonGroup } from "@chakra-ui/react";
import SingleCoverOn from "../../../assets/Settings/SingleCoverOn.png";
import SingleCoverOff from "../../../assets/Settings/SingleCoverOff.png";
import SectionItem from "../SectionItem";
import { useT } from "../../../locales";
import { useConfig } from "../../../utils/config";
import ImageRadioButton from "./ImageRadioButton";
import { trackEvent } from "../../../utils/umami";

const SingleCover = () => {
  const t = useT();
  const [value, setValue] = useConfig("bookSingleCover");
  const [imagesPerRow] = useConfig("bookImagesPerRow");

  return (
    <SectionItem
      title={t("Settings.Reader.SingleCover.title")}
      description={t("Settings.Reader.SingleCover.description")}
    >
      <ButtonGroup>
        <ImageRadioButton
          src={SingleCoverOn}
          isChecked={imagesPerRow !== 2 || value}
          onClick={() => {
            setValue(true);
            trackEvent("settings", `singleCover${true}`);
          }}
          disabled={imagesPerRow !== 2}
        >
          {t("Settings.Reader.SingleCover.toggle", { value: true })}
        </ImageRadioButton>

        <ImageRadioButton
          src={SingleCoverOff}
          isChecked={imagesPerRow === 2 && !value}
          onClick={() => {
            setValue(false);
            trackEvent("settings", `singleCover${false}`);
          }}
          disabled={imagesPerRow !== 2}
        >
          {t("Settings.Reader.SingleCover.toggle", { value: false })}
        </ImageRadioButton>
      </ButtonGroup>
    </SectionItem>
  );
};

export default memo(SingleCover);
