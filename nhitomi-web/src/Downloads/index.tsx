import React, { ReactNode, useMemo } from "react";
import { PrefetchGenerator, PrefetchLink, TypedPrefetchLinkProps, usePostfetch } from "../Prefetch";
import { PageContainer } from "../Components/PageContainer";
import { useTabTitle } from "../TitleSetter";
import { useLocalized } from "../LocaleManager";
import { Container } from "../Components/Container";
import { useDownloads } from "../DownloadManager";
import { FormattedMessage } from "react-intl";
import { cx } from "emotion";
import { BookTaskDisplay } from "./BookTaskDisplay";
import { useLayout } from "../LayoutManager";
import { EmptyIndicator } from "../Components/EmptyIndicator";

export type PrefetchResult = {};
export type PrefetchOptions = {};

export const useDownloadsPrefetch: PrefetchGenerator<PrefetchResult, PrefetchOptions> = () => {
  return {
    destination: {
      path: "/downloads",
    },

    fetch: async () => ({}),
  };
};

export const DownloadsLink = ({ ...props }: TypedPrefetchLinkProps & PrefetchOptions) => (
  <PrefetchLink fetch={useDownloadsPrefetch} options={{}} {...props} />
);

export const Downloads = (options: PrefetchOptions) => {
  const { result } = usePostfetch(useDownloadsPrefetch, {
    requireAuth: true,
    ...options,
  });

  if (!result) return null;

  return (
    <PageContainer>
      <Loaded {...result} />
    </PageContainer>
  );
};

const Loaded = () => {
  useTabTitle(useLocalized("pages.downloads.title"));

  const { screen } = useLayout();
  const { tasks } = useDownloads();

  return (
    <Container className="divide-y divide-gray-darkest">
      {useMemo(
        () => (
          <div className="p-4">
            <div className="text-2xl">
              <FormattedMessage id="pages.downloads.title" />
            </div>
            <div className="text-sm text-gray-darker">
              <FormattedMessage
                id="pages.downloads.subtitle"
                values={{ count: tasks.filter((t) => t.state.type === "running").length }}
              />
            </div>
          </div>
        ),
        []
      )}

      <div>
        {!tasks.length && (
          <div className="p-4">
            <EmptyIndicator>
              <FormattedMessage id="pages.downloads.empty" />
            </EmptyIndicator>
          </div>
        )}

        {useMemo(
          () =>
            tasks.map((task) => {
              let node: ReactNode;

              switch (task.target.type) {
                case "book":
                  node = <BookTaskDisplay task={task} />;
                  break;
              }

              return (
                <div
                  key={task.id}
                  className={cx({ "w-1/2": screen === "lg", "w-full": screen !== "lg" }, "inline-flex")}
                >
                  {node}
                </div>
              );
            }),
          [tasks]
        )}
      </div>
    </Container>
  );
};
