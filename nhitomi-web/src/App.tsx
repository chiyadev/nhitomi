import React, { useEffect, useMemo, useRef } from "react";
import { Redirect, Route, Router, Switch } from "react-router-dom";
import { Navigator, useNavigator } from "./state";
import { ProgressManager } from "./ProgressManager";
import { trackView } from "./track";
import { useErrorBoundary } from "preact/hooks";
import { captureException } from "@sentry/react";

import { ConfigManager } from "./ConfigManager";
import { LayoutManager } from "./LayoutManager";
import { ClientManager } from "./ClientManager";
import { LocaleManager } from "./LocaleManager";
import { NotificationManager } from "./NotificationManager";
import { DownloadManager } from "./DownloadManager";
import { PrefetchScrollPreserver } from "./Prefetch";
import { AnimationSetter } from "./AnimationSetter";
import { BlurSetter } from "./BlurSetter";
import { TitleSetter } from "./TitleSetter";
import { Sidebar } from "./Sidebar";
import { MaintenanceHeader } from "./MaintenanceHeader";
import { Footer } from "./Footer";
import { ShortcutHintDisplay } from "./ShortcutHintDisplay";

import { About } from "./About";
import { Support } from "./Support";
import { Pending as SupportPending } from "./Support/Pending";
import { Authentication } from "./Authentication";
import { OAuthCallback } from "./Authentication/OAuthCallback";
import { OAuthRedirect } from "./Authentication/OAuthRedirect";
import { BookListing } from "./BookListing";
import { BookReader } from "./BookReader";
import { CollectionContent } from "./CollectionContent";
import { CollectionEdit } from "./CollectionListing/Edit";
import { CollectionListing } from "./CollectionListing";
import { Downloads } from "./Downloads";
import { Settings } from "./Settings";
import { Debug } from "./Internal/Debug";
import { NotFound } from "./NotFound";

export const App = () => {
  const [error, reset] = useErrorBoundary((e) => {
    captureException(e);
  });

  if (error) {
    return <ErrorDisplay error={error} reset={reset} />;
  }

  return (
    <Router history={Navigator.history}>
      <ProgressManager>
        <ConfigManager>
          <LayoutManager>
            <ClientManager>
              <LocaleManager>
                <NotificationManager>
                  <DownloadManager>
                    <PrefetchScrollPreserver />
                    <AnimationSetter />
                    <BlurSetter />
                    <TitleSetter />
                    <ShortcutHintDisplay />

                    <Sidebar>
                      <div className="flex flex-col min-h-screen">
                        <div className="relative w-full flex-1">
                          <MaintenanceHeader />

                          <Routing />
                        </div>
                        <div className="mt-4">
                          <Footer />
                        </div>
                      </div>
                    </Sidebar>
                  </DownloadManager>
                </NotificationManager>
              </LocaleManager>
            </ClientManager>
          </LayoutManager>
        </ConfigManager>
      </ProgressManager>
    </Router>
  );
};

const Routing = () => {
  const { path } = useNavigator();
  const tracked = useRef<number>();

  useEffect(() => {
    // timeout prevents multiple page views being tracked for redirects
    clearTimeout(tracked.current);

    // do not include queries in metrics, because it pollutes the view
    tracked.current = window.setTimeout(() => trackView(path), 1000);
  }, [path]);

  return useMemo(
    () => (
      <Switch location={{ pathname: path, search: "", hash: "", state: undefined }}>
        <Route path="/" exact>
          <Redirect to="/books" />
        </Route>
        <Route path="/about" exact component={About} />

        <Route path="/support" exact component={Support} />
        <Route path="/support/pending" exact component={SupportPending} />

        <Route path="/auth" exact component={Authentication} />
        <Route
          path="/oauth/:service"
          exact
          render={({
            match: {
              params: { service },
            },
          }) => <OAuthCallback service={service} />}
        />
        <Route
          path="/oauth/redirect/:id"
          exact
          render={({
            match: {
              params: { id },
            },
          }) => <OAuthRedirect id={id} />}
        />

        <Route path="/books" exact component={BookListing} />
        <Route
          path="/books/:id"
          exact
          render={({
            match: {
              params: { id },
            },
          }) => <BookReader id={id} />}
        />
        <Route
          path="/books/:id/contents/:contentId"
          exact
          render={({
            match: {
              params: { id, contentId },
            },
          }) => <BookReader id={id} contentId={contentId} />}
        />

        <Route
          path="/collections/:id"
          exact
          render={({
            match: {
              params: { id },
            },
          }) => <CollectionContent id={id} />}
        />
        <Route
          path="/collections/:id/edit"
          exact
          render={({
            match: {
              params: { id },
            },
          }) => <CollectionEdit id={id} />}
        />

        <Route
          path="/users/:id/collections"
          exact
          render={({
            match: {
              params: { id },
            },
          }) => <CollectionListing id={id} />}
        />

        <Route path="/downloads" exact component={Downloads} />

        <Route path="/settings" exact component={Settings} />
        <Route path="/settings/debug" exact component={Debug} />

        <Route component={NotFound} />
      </Switch>
    ),
    [path]
  );
};

const ErrorDisplay = ({ error }: { error: Error; reset: () => void }) => {
  return (
    <div className="p-4">
      <div>nhitomi has encountered a critical error.</div>
      <div>This error has been automatically reported to the developers. Please try again later!</div>

      <br />
      <div className="text-sm whitespace-pre">
        <code>{error.stack}</code>
      </div>
    </div>
  );
};
