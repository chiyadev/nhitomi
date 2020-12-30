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
      const redirectUri = url.searchParams.get("redirect_uri");

      if (redirectUri) {
        const redirectUrl = new URL(redirectUri);

        // support for mirror domain authentication
        redirectUrl.protocol = window.location.protocol;
        redirectUrl.host = window.location.host;

        url.searchParams.set("redirect_uri", redirectUrl.href);
      }

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
