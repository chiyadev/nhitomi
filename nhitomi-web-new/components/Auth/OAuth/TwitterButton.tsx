import React, { memo } from "react";
import { Button, Icon, LightMode } from "@chakra-ui/react";
import { FaTwitter } from "react-icons/fa";

const TwitterButton = () => {
  return (
    <LightMode>
      <Button as="a" size="sm" colorScheme="twitter" disabled leftIcon={<Icon as={FaTwitter} />}>
        Continue with Twitter
      </Button>
    </LightMode>
  );
};

export default memo(TwitterButton);
