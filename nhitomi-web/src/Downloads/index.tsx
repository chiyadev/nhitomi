import React, { ReactNode, useMemo } from "react";
import { PrefetchGenerator, PrefetchLink, TypedPrefetchLinkProps, usePostfetch } from "../Prefetch";
import { PageContainer } from "../Components/PageContainer";
import { useTabTitle } from "../TitleSetter";
import { useLocalized } from "../LocaleManager";
import { Container } from "../Components/Container";
import { useDownloads } from "../DownloadManager";
import { FormattedMessage } from "react-intl";
import { animated, useTransition } from "react-spring";
import { cx } from "emotion";
import { BookTaskDisplay } from "./BookTaskDisplay";
import { useLayout } from "../LayoutManager";

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

  const [transitions] = useTransition(
    tasks,
    {
      from: { opacity: 0 },
      enter: { opacity: 1 },
      leave: { opacity: 0 },
    },
    [tasks]
  );

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
        {transitions((style, task) => {
          let node: ReactNode;

          switch (task.target.type) {
            case "book":
              node = <BookTaskDisplay task={task} />;
              break;
          }

          return (
            <animated.div style={style} key={task.id} className={cx({ "w-1/2": screen === "lg" }, "inline-flex")}>
              {node}
            </animated.div>
          );
        })}
      </div>
    </Container>
  );
};
