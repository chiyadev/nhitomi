import React, { memo } from "react";
import { useT } from "../../locales";
import { useClientInfoAuth } from "../../utils/client";
import { userHasPermissions } from "../../utils/perms";
import { UserPermissions } from "nhitomi-api";
import Layout from "../Layout";
import Header from "../Header";
import HeaderTitle from "./HeaderTitle";
import LayoutBody from "../LayoutBody";
import { VStack } from "@chakra-ui/layout";
import Section from "./Section";
import { FaBook, FaImage, FaUser, FaWrench } from "react-icons/fa";
import Account from "./User/Account";
import Token from "./User/Token";
import DisplayLanguage from "./Appearance/DisplayLanguage";
import SearchLanguages from "./Appearance/SearchLanguages";
import ForceInfoOverlay from "./Appearance/ForceInfoOverlay";
import ViewportBound from "./Reader/ViewportBound";
import ImagesPerRow from "./Reader/ImagesPerRow";
import LeftToRight from "./Reader/LeftToRight";
import SingleCover from "./Reader/SingleCover";
import Maintenance from "./Debug/Maintenance";
import SfwMode from "./Debug/SfwMode";

const Settings = () => {
  const t = useT();
  const info = useClientInfoAuth();

  const debug =
    process.env.NODE_ENV === "development" ||
    (info?.user && userHasPermissions(info.user, UserPermissions.ManageServer));

  return (
    <Layout title={[t("Settings.title")]}>
      <Header title={<HeaderTitle />} />

      <LayoutBody p={4}>
        <VStack align="stretch" spacing={12}>
          <Section name={t("Settings.user")} icon={FaUser}>
            <Account />
            <Token />
          </Section>

          <Section name={t("Settings.appearance")} icon={FaImage}>
            <DisplayLanguage />
            <SearchLanguages />
            <ForceInfoOverlay />
          </Section>

          <Section name={t("Settings.reader")} icon={FaBook}>
            <ViewportBound />
            <ImagesPerRow />
            <LeftToRight />
            <SingleCover />
          </Section>

          {debug && (
            <Section name={t("Settings.debug")} icon={FaWrench}>
              <Maintenance />
              <SfwMode />
            </Section>
          )}
        </VStack>
      </LayoutBody>
    </Layout>
  );
};

export default memo(Settings);
