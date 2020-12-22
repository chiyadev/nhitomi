import React, { Dispatch, memo } from "react";
import {
  Drawer,
  DrawerBody,
  DrawerCloseButton,
  DrawerContent,
  DrawerHeader,
  DrawerOverlay,
  VStack,
} from "@chakra-ui/react";
import { useT } from "../../../locales";
import SortRadio from "./SortRadio";
import OrderRadio from "./OrderRadio";
import SourceCheck from "./SourceCheck";

const QueryDrawer = ({ open, setOpen }: { open: boolean; setOpen: Dispatch<boolean> }) => {
  const t = useT();

  return (
    <Drawer placement="right" isOpen={open} onClose={() => setOpen(false)}>
      <DrawerOverlay>
        <DrawerContent>
          <DrawerCloseButton />
          <DrawerHeader>{t("BookListing.QueryDrawer.title")}</DrawerHeader>

          <DrawerBody>
            <VStack align="stretch" spacing={8}>
              <SortRadio setOpen={setOpen} />
              <OrderRadio setOpen={setOpen} />
              <SourceCheck setOpen={setOpen} />
            </VStack>
          </DrawerBody>
        </DrawerContent>
      </DrawerOverlay>
    </Drawer>
  );
};

export default memo(QueryDrawer);
