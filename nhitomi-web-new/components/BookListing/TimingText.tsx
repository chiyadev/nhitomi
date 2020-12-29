import React, { memo } from "react";
import { BookSearchResult } from "nhitomi-api";
import { chakra } from "@chakra-ui/react";
import { useT } from "../../locales";

const TimingText = ({ result }: { result: BookSearchResult }) => {
  const t = useT();

  return (
    <chakra.div px={2} color="gray.500" fontSize="sm">
      {t("BookListing.TimingText.text", {
        count: result.total,
        time: Math.round(parseFloat(result.took.split(":").slice(-1)[0]) * 100) / 100,
      })}
    </chakra.div>
  );
};

export default memo(TimingText);
