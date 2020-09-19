import React, { useLayoutEffect } from "react";
import { useInterval } from "react-use";
import { useClientInfo } from "../ClientManager";
import { HomeOutlined, Loading3QuartersOutlined } from "@ant-design/icons";
import { FormattedMessage } from "react-intl";
import { Tooltip } from "../Components/Tooltip";
import { BookListingLink } from "../BookListing";
import { RoundIconButton } from "../Components/RoundIconButton";
import { PageContainer } from "../Components/PageContainer";
import { usePrefetch } from "../Prefetch";
import { useSupportPrefetch } from ".";

export const Pending = () => {
  const { info, fetchInfo } = useClientInfo();
  const [, navigateSupport] = usePrefetch(useSupportPrefetch, {});

  useInterval(async () => {
    try {
      await fetchInfo();
    } catch (e) {
      console.warn("could not reload info", e);
    }
  }, 5000);

  // just wait until user turns into supporter
  useLayoutEffect(() => {
    if (info.authenticated && info.user.isSupporter) navigateSupport("replace");
  }, [info, navigateSupport]);

  return (
    <PageContainer>
      <div className="absolute transform-center text-center space-y-4">
        <div className="text-xl">
          <Loading3QuartersOutlined className="animate-spin" />
        </div>

        <div>
          <FormattedMessage id="pages.support.pending.text" />
        </div>

        <div className="text-sm">
          <div>
            <FormattedMessage id="pages.support.pending.desc1" />
          </div>
          <div>
            <FormattedMessage id="pages.support.pending.desc2" />
          </div>
        </div>

        <div className="flex flex-row justify-center">
          <Tooltip
            placement="bottom"
            overlay={<FormattedMessage id="pages.support.pending.home" />}
          >
            <BookListingLink>
              <RoundIconButton>
                <HomeOutlined />
              </RoundIconButton>
            </BookListingLink>
          </Tooltip>
        </div>
      </div>
    </PageContainer>
  );
};
