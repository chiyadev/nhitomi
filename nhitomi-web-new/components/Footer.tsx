import React, { memo } from "react";
import { FaCode, FaDiscord, FaGithub } from "react-icons/fa";
import { HStack, Icon, Link, Tooltip } from "@chakra-ui/react";
import { useT } from "../locales";

const Footer = () => {
  const t = useT();

  return (
    <HStack as="footer" spacing={4} justify="center" pt={4} pb={4} textAlign="center" color="gray.500">
      <Tooltip label={t("Footer.github")}>
        <Link href="https://github.com/chiyadev/nhitomi" isExternal>
          <Icon as={FaGithub} />
        </Link>
      </Tooltip>

      <Tooltip label={t("Footer.discord")}>
        <Link href="https://discord.gg/JFNga7q" isExternal>
          <Icon as={FaDiscord} />
        </Link>
      </Tooltip>

      <Tooltip label={t("Footer.api")}>
        <Link href="https://github.com/chiyadev/nhitomi/wiki/API" isExternal>
          <Icon as={FaCode} />
        </Link>
      </Tooltip>
    </HStack>
  );
};

export default memo(Footer);
