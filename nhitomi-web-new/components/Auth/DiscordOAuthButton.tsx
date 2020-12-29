import React, { memo } from "react";
import { Button } from "@chakra-ui/react";
import { useClientInfo } from "../../utils/client";
import { useWindowValue } from "../../utils/hooks";

const DiscordOAuthButton = () => {
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
    <Button as="a" href={url} disabled={!url}>
      Sign in
    </Button>
  );
};

export default memo(DiscordOAuthButton);
