import React, { memo } from "react";
import { Button, Icon, LightMode } from "@chakra-ui/react";
import { useClientInfo } from "../../../utils/client";
import { useWindowValue } from "../../../utils/hooks";
import { FaDiscord } from "react-icons/fa";

const DiscordButton = () => {
  const info = useClientInfo();

  const url = useWindowValue(
    info?.discordOAuthUrl,
    () => {
      if (!info?.discordOAuthUrl) {
        return;
      }

      const url = new URL(info.discordOAuthUrl);

      // override redirect uri for mirror domain support
      const redirectUri = new URL("/oauth/discord", window.location.href).href;

      url.searchParams.set("redirect_uri", redirectUri);
      url.searchParams.set("state", btoa(JSON.stringify({ redirectUri })));

      return url.href;
    },
    [info]
  );

  return (
    <LightMode>
      <Button as="a" size="sm" colorScheme="discord" href={url} disabled={!url} leftIcon={<Icon as={FaDiscord} />}>
        Continue with Discord
      </Button>
    </LightMode>
  );
};

export default memo(DiscordButton);
