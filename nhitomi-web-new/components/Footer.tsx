import React, { memo } from "react";
import { FaCode, FaDiscord, FaGithub } from "react-icons/fa";
import { HStack, Icon, Tooltip } from "@chakra-ui/react";

const Footer = () => {
  return (
    <HStack as="footer" spacing={4} justify="center" pt={4} pb={4} textAlign="center" textColor="gray.500">
      <Tooltip label="GitHub">
        <a href="https://github.com/chiyadev/nhitomi">
          <Icon as={FaGithub} />
        </a>
      </Tooltip>

      <Tooltip label="Discord">
        <a href="https://discord.gg/JFNga7q">
          <Icon as={FaDiscord} />
        </a>
      </Tooltip>

      <Tooltip label="API">
        <a href="https://github.com/chiyadev/nhitomi/wiki/API">
          <Icon as={FaCode} />
        </a>
      </Tooltip>
    </HStack>
  );
};

export default memo(Footer);
