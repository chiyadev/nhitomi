import {
  BookApi,
  CollectionApi,
  Configuration,
  ConfigurationParameters,
  DownloadApi,
  GetInfoAuthenticatedResponse,
  InfoApi,
  InternalApi,
  UserApi,
  ValidationProblemArrayResult,
} from "nhitomi-api";
import { ValidationError } from "./errors";
import node_fetch from "node-fetch";
import config from "config";
import { Counter } from "prom-client";
import { URL } from "url";

const requestCount = new Counter({
  name: "api_requests",
  help: "Number of API requests.",
});

const responseErrorCount = new Counter({
  name: "api_response_errors",
  help: "Number of API error responses.",
});

export class ApiClient {
  readonly httpConfig: ConfigurationParameters = {
    fetchApi: async (input: any, init: any) => {
      for (let i = 0; ; i++) {
        if (i) {
          await new Promise((resolve) => setTimeout(resolve, 1000));
        }

        try {
          return await (node_fetch as any)(input, { ...init, highWaterMark: 1024 * 1024 * 1024 });
        } catch (e) {
          // retry up to three times
          if (i < 3) continue;

          throw e;
        }
      }
    },

    middleware: [
      {
        pre: async () => {
          requestCount.inc();
        },
        post: async (context) => {
          const { response } = context;
          if (response.ok) return;

          responseErrorCount.inc();

          // validation failure (unprocessable entity)
          if (response.status === 422) {
            const { value }: ValidationProblemArrayResult = await response.json();

            throw new ValidationError(value);
          }

          throw Error((await response.json())?.message || response.statusText);
        },
      },
    ],
  };

  readonly user: UserApi;
  readonly info: InfoApi;
  readonly book: BookApi;
  readonly collection: CollectionApi;
  readonly download: DownloadApi;
  readonly internal: InternalApi;

  constructor(readonly baseUrl: string, readonly token?: string) {
    this.user = new UserApi(new Configuration(this.httpConfig));
    this.info = new InfoApi(new Configuration(this.httpConfig));
    this.book = new BookApi(new Configuration(this.httpConfig));
    this.collection = new CollectionApi(new Configuration(this.httpConfig));
    this.download = new DownloadApi(new Configuration(this.httpConfig));
    this.internal = new InternalApi(new Configuration(this.httpConfig));

    this.httpConfig.basePath = baseUrl;
    this.httpConfig.accessToken = token;
  }
}

export function createApiClient(token?: string) {
  return new ApiClient(
    config.get<string>("api.baseUrl") || "https://nhitomi.chiya.dev/api/v1",
    token || config.get<string>("api.token")
  );
}

export let CurrentInfo: GetInfoAuthenticatedResponse | undefined;

/** Formats a link to an API route using publicUrl. */
export function getApiLink(path: string): string {
  return new URL(path, `${CurrentInfo?.publicUrl}/api/v1/`).href;
}

/** Formats a link to a frontend route using publicUrl. */
export function getWebLink(path: string): string {
  return new URL(path, CurrentInfo?.publicUrl).href;
}

(async () => {
  while (true) {
    try {
      const client = createApiClient();
      CurrentInfo = await client.info.getInfoAuthenticated();
    } catch (e) {
      console.warn("could not retrieve api info", e);
    }

    await new Promise((resolve) => setTimeout(resolve, 30000));
  }
})();
