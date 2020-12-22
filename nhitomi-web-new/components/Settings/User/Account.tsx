import React, { memo } from "react";
import SectionItem from "../SectionItem";
import { useT } from "../../../locales";
import { useClientInfoAuth } from "../../../utils/client";
import { Button, ButtonGroup, Icon } from "@chakra-ui/react";
import { FaSignOutAlt } from "react-icons/fa";
import { useConfig } from "../../../utils/config";
import { useRouter } from "next/router";

const Account = () => {
  const t = useT();
  const info = useClientInfoAuth();
  const router = useRouter();
  const [, setToken] = useConfig("token");

  return (
    <SectionItem
      title={t("Settings.User.Account.title")}
      description={t("Settings.User.Account.description", { username: info?.user.username })}
    >
      <ButtonGroup>
        <Button
          leftIcon={<Icon as={FaSignOutAlt} />}
          onClick={() => {
            setToken(undefined);
            router.reload();
          }}
        >
          {t("Settings.User.Account.signOut")}
        </Button>
      </ButtonGroup>
    </SectionItem>
  );
};

export default memo(Account);
