import React, { memo } from "react";
import { CookieContainer, parseConfigs } from "../utils/config";
import { GetServerSideProps } from "next";
import { parseCookies } from "nookies";
import { createApiClient, useClientInfoAuth } from "../utils/client";
import {
  GetInfoAuthenticatedResponse,
  GetInfoAuthenticatedResponseFromJSON,
  GetInfoAuthenticatedResponseToJSON,
  UserPermissions,
} from "nhitomi-api";
import { sanitizeProps } from "../utils/props";
import { useChangeCount } from "../utils/hooks";
import ConfigProvider from "../components/ConfigProvider";
import ErrorPage from "../components/ErrorPage";
import { useT } from "../locales";
import Layout from "../components/Layout";
import Header from "../components/Header";
import HeaderTitle from "../components/Settings/HeaderTitle";
import LayoutBody from "../components/LayoutBody";
import Section from "../components/Settings/Section";
import { FaBook, FaImage, FaUser, FaWrench } from "react-icons/fa";
import Account from "../components/Settings/User/Account";
import { VStack } from "@chakra-ui/layout";
import Token from "../components/Settings/User/Token";
import DisplayLanguage from "../components/Settings/Appearance/DisplayLanguage";
import SearchLanguages from "../components/Settings/Appearance/SearchLanguages";
import { userHasPermissions } from "../utils/perms";
import Maintenance from "../components/Settings/Debug/Maintenance";
import SfwMode from "../components/Settings/Debug/SfwMode";
import ViewportBound from "../components/Settings/Reader/ViewportBound";
import LeftToRight from "../components/Settings/Reader/LeftToRight";
import ImagesPerRow from "../components/Settings/Reader/ImagesPerRow";
import SingleCover from "../components/Settings/Reader/SingleCover";
import ForceInfoOverlay from "../components/Settings/Appearance/ForceInfoOverlay";

type Props = {
  cookies: CookieContainer;
  result:
    | {
        type: "success";
        info: GetInfoAuthenticatedResponse;
      }
    | {
        type: "error";
        message: string;
      };
};

export const getServerSideProps: GetServerSideProps<Props> = async (ctx) => {
  const cookies = parseCookies(ctx);
  const { token } = parseConfigs(cookies);

  try {
    const client = createApiClient(token);

    if (!client) {
      return {
        redirect: {
          destination: "/auth",
          permanent: false,
        },
      };
    }

    const info = await client.info.getInfoAuthenticated();

    return {
      props: sanitizeProps({
        cookies,
        result: {
          type: "success",
          info: GetInfoAuthenticatedResponseToJSON(info),
        },
      }),
    };
  } catch (e) {
    ctx.res.statusCode = 500;

    return {
      props: sanitizeProps({
        cookies,
        result: {
          type: "error",
          message: e.message,
        },
      }),
    };
  }
};

const Settings = ({ cookies, result }: Props) => {
  const renderId = useChangeCount(result);

  switch (result.type) {
    case "success":
      return (
        <ConfigProvider key={renderId} cookies={cookies} info={GetInfoAuthenticatedResponseFromJSON(result.info)}>
          <Content />
        </ConfigProvider>
      );

    case "error":
      return (
        <ConfigProvider key={renderId} cookies={cookies}>
          <ErrorPage message={result.message} />
        </ConfigProvider>
      );
  }
};

const Content = () => {
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
