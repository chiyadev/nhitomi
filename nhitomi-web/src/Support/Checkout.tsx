import React, { useCallback, useState } from "react";
import { Slider } from "../Components/Slider";
import { usePageState } from "../state";
import { getColor } from "../theme";
import { FormattedMessage } from "react-intl";
import { animated, useSpring } from "react-spring";
import { css, cx } from "emotion";
import { PrefetchResult } from ".";
import { Disableable } from "../Components/Disableable";
import { Loading3QuartersOutlined } from "@ant-design/icons";
import { useNotify } from "../NotificationManager";
import { useClient, useClientInfo } from "../ClientManager";
import { loadStripe } from "@stripe/stripe-js";
import { trackEvent } from "../umami";

export const Checkout = ({ supporterPrice, apiKey }: PrefetchResult) => {
  const client = useClient();
  const { info } = useClientInfo();
  const { notifyError } = useNotify();

  const [loading, setLoading] = useState(false);
  const [duration, setDuration] = usePageState("duration", 2);
  const amount = duration * supporterPrice;

  const submit = useCallback(async () => {
    trackEvent("action", "supportCheckout");
    setLoading(true);

    try {
      if (!info.authenticated) throw Error("Unauthorized.");

      const stripe = await loadStripe(apiKey);

      if (!stripe) throw Error("Could not load Stripe.");

      const { sessionId } = await client.user.createUserSupporterCheckout({
        id: info.user.id,
        createSupporterCheckoutRequest: { amount },
      });

      const { error } = await stripe.redirectToCheckout({ sessionId });

      if (error) throw Error(error.message || "Unknown Stripe error.");
    } catch (e) {
      notifyError(e);
    } finally {
      setLoading(false);
    }
  }, [amount, apiKey, client, info, notifyError]);

  return (
    <div className="space-y-8">
      <div className="text-lg text-center">
        <span className="shadow-lg rounded bg-pink text-white p-2">— Let&apos;s become one! —</span>
      </div>

      <Disableable disabled={loading}>
        <div className="max-w-xs mx-auto p-4 space-y-4">
          <CheckoutButton duration={duration} loading={loading} submit={submit} />

          <div className="space-y-1">
            <div className="text-center text-sm">{amount} USD</div>

            <Slider
              className="w-full"
              color={getColor("pink")}
              min={0}
              max={12}
              value={duration}
              setValue={(v) => setDuration(Math.max(1, v))}
              overlay={`${amount} USD`}
            />
          </div>

          <ul className="list-disc list-inside text-sm">
            <li>nhitomi supporter is a non-recurring payment.</li>
            <li>If you are already a supporter, your supporter period will be extended.</li>
          </ul>
        </div>
      </Disableable>
    </div>
  );
};

const CheckoutButton = ({ duration, loading, submit }: { duration: number; loading: boolean; submit: () => void }) => {
  const { info } = useClientInfo();
  const supporter = info.authenticated && info.user.isSupporter;
  const [hover, setHover] = useState(false);

  const imageStyle = useSpring({
    opacity: hover || loading ? 0.6 : 0.5,
    transform: `translate(-50%, -50%) scale(${hover || loading ? 1.1 : 1})`,
  });

  const textStyle = useSpring({
    opacity: loading ? 0 : 1,
    transform: `scale(${loading ? 1.1 : 1})`,
  });

  const loadingStyle = useSpring({
    opacity: loading ? 1 : 0,
    transform: `scale(${loading ? 1 : 0.9})`,
  });

  return (
    <div
      className="h-32 bg-black rounded-lg relative overflow-hidden cursor-pointer shadow-lg border border-pink"
      onClick={submit}
      onMouseEnter={() => setHover(true)}
      onMouseLeave={() => setHover(false)}
    >
      <animated.img
        style={imageStyle}
        alt="buttonbg"
        src="/assets/images/megumi_button_bg.jpg"
        className={cx(
          "absolute w-full object-cover select-none pointer-events-none",
          css`
            left: 50%;
            top: 50%;
          `
        )}
      />

      <div className="absolute transform-center w-full text-center">
        <animated.div style={textStyle}>
          <div className="text-xl">
            <FormattedMessage id="pages.support.buy" />
          </div>
          <div className="text-sm">
            <FormattedMessage
              id={supporter ? "pages.support.duration_supporter" : "pages.support.duration"}
              values={{ duration }}
            />
          </div>
        </animated.div>
      </div>

      <animated.span style={loadingStyle}>
        <span className="absolute transform-center text-xl">
          <Loading3QuartersOutlined className="animate-spin" />
        </span>
      </animated.span>
    </div>
  );
};
