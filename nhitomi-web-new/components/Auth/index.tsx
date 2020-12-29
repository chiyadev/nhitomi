import React, { memo } from "react";
import { useT } from "../../locales";
import Layout from "../Layout";
import LayoutBody from "../LayoutBody";
import DiscordOAuthButton from "./DiscordOAuthButton";

const Auth = () => {
  const t = useT();

  return (
    <Layout showFooter={false}>
      <LayoutBody>
        <DiscordOAuthButton />
      </LayoutBody>
    </Layout>
  );
};

export default memo(Auth);
