import React, { memo, useState } from "react";
import { useT } from "../../../locales";
import { useConfig } from "../../../utils/config";
import SectionItem from "../SectionItem";
import { chakra, Fade, HStack, Icon, Link, Tooltip, useClipboard, VStack } from "@chakra-ui/react";
import { FaExclamationTriangle, FaLink } from "react-icons/fa";
import { trackEvent } from "../../../utils/umami";

const Token = () => {
  const t = useT();
  const [token] = useConfig("token");
  const [visible, setVisible] = useState(false);
  const { onCopy, hasCopied } = useClipboard(token || "");

  return (
    <SectionItem title={t("Settings.User.Token.title")} description={t("Settings.User.Token.description")}>
      <Link href="https://github.com/chiyadev/nhitomi/wiki/API" color="blue.300" isExternal>
        <HStack spacing={2}>
          <Icon as={FaLink} />
          <div>{t("Settings.User.Token.docLink")}</div>
        </HStack>
      </Link>

      <VStack w="full" align="start" spacing={1}>
        <HStack w="full" spacing={2}>
          <chakra.div flexShrink={0}>{t("Settings.User.Token.token")}</chakra.div>

          <Tooltip
            label={t(hasCopied ? "Settings.User.Token.copied" : "Settings.User.Token.copy")}
            closeOnClick={false}
          >
            <Link
              as="button"
              px={2}
              py={1}
              bg="rgba(255, 255, 255, 0.08)"
              borderRadius="md"
              color={visible ? undefined : "transparent"}
              style={{ textDecoration: "none" }}
              isTruncated
              onClick={() => {
                setVisible(true);
                onCopy();

                trackEvent("settings", "tokenCopy");
              }}
            >
              {token}
            </Link>
          </Tooltip>
        </HStack>

        <Fade in={visible} unmountOnExit>
          <HStack spacing={2} fontSize="sm" color="red.300">
            <Icon as={FaExclamationTriangle} />
            <div>{t("Settings.User.Token.warning")}</div>
          </HStack>
        </Fade>
      </VStack>
    </SectionItem>
  );
};

export default memo(Token);
