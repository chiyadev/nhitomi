import React, { memo, useCallback } from "react";
import { Modal, ModalContent, ModalOverlay } from "@chakra-ui/react";
import Content from "./Content";
import { useQuery } from "../../../utils/query";
import Router from "next/router";

const SearchOverlay = ({ open, setOpen }: { open: boolean; setOpen: (value: boolean) => void }) => {
  const [value, setValue] = useQuery("query");

  return (
    <Modal isOpen={open} onClose={() => setOpen(false)} motionPreset="slideInBottom" size="xl">
      <ModalOverlay />
      <ModalContent ml={4} mr={4} mt={8}>
        <Content
          value={value}
          setValue={useCallback(
            async (value) => {
              if (Router.pathname === "/books") {
                await setValue(value, "push");
              } else {
                await Router.push({
                  pathname: "/books",
                  query: {
                    query: value,
                  },
                });
              }
            },
            [setValue]
          )}
        />
      </ModalContent>
    </Modal>
  );
};

export default memo(SearchOverlay);
