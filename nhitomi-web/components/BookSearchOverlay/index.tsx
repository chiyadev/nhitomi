import React, { memo, useCallback } from "react";
import { Modal, ModalContent, ModalOverlay } from "@chakra-ui/react";
import Content from "./Content";
import { useQuery } from "../../utils/query";
import { useRouter } from "next/router";
import { useHotkeys } from "react-hotkeys-hook";

const BookSearchOverlay = ({ open, setOpen }: { open: boolean; setOpen: (value: boolean) => void }) => {
  const router = useRouter();
  const [value, setValue] = useQuery("query");

  useHotkeys(
    "f",
    (e) => {
      e.preventDefault();
      setOpen(true);
    },
    [setOpen]
  );

  return (
    <Modal isOpen={open} onClose={() => setOpen(false)} motionPreset="slideInBottom" size="xl">
      <ModalOverlay />
      <ModalContent mx={4}>
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

export default memo(BookSearchOverlay);
