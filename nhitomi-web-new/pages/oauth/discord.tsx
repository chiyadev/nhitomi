import { GetServerSideProps } from "next";
import { createApiClient } from "../../utils/client";
import { destroyCookie, setCookie } from "nookies";

export const getServerSideProps: GetServerSideProps = async (ctx) => {
  try {
    const client = createApiClient();
    const { code, state } = ctx.query;

    const { redirectUri } = JSON.parse(atob(Array.isArray(state) ? state[0] : state));

    const { token } = await client.user.authenticateUserDiscord({
      authenticateDiscordRequest: {
        code: Array.isArray(code) ? code[0] : code,
        redirectUri,
      },
    });

    setCookie(ctx, "token", JSON.stringify(token), {
      path: "/",
      sameSite: "lax",
      secure: process.env.NODE_ENV !== "development",
      expires: new Date(2100, 1, 1),
    });

    return {
      redirect: {
        destination: "/",
        permanent: false,
      },
    };
  } catch (e) {
    console.error(e);
  }

  destroyCookie(ctx, "token");

  return {
    redirect: {
      destination: "/auth",
      permanent: false,
    },
  };
};

const OAuthDiscord = () => null;
export default OAuthDiscord;
