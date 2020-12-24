import React, { memo, useCallback } from "react";
import { Modal, ModalContent, ModalOverlay } from "@chakra-ui/react";
import Content from "./Content";
import { useQuery } from "../../../utils/query";
import { useRouter } from "next/router";

const SearchOverlay = ({ open, setOpen }: { open: boolean; setOpen: (value: boolean) => void }) => {
  const router = useRouter();
  const [value, setValue] = useQuery("query");

  return (
    <Modal isOpen={open} onClose={() => setOpen(false)} motionPreset="slideInBottom" size="xl">
      <ModalOverlay />
      <ModalContent>
        <Content
          value={value}
          setValue={useCallback(
            async (value) => {
              if (router.pathname === "/books") {
                await setValue(value, "push");
              } else {
                await router.push({
                  pathname: "/books",
                  query: {
                    query: value || [],
                  },
                });
              }
            },
            [router, setValue]
          )}
        />
      </ModalContent>
    </Modal>
  );
};

export default memo(SearchOverlay);
