import {
  BASE_PATH,
  BookApi,
  CollectionApi,
  Configuration,
  ConfigurationParameters,
  GetInfoAuthenticatedResponse,
  InfoApi,
  InternalApi,
  UserApi,
} from "nhitomi-api";
import config from "config";
import fetch from "node-fetch";
import { URL } from "url";
import { Counter, Gauge } from "prom-client";

type ApiClientCore = {
  readonly config: ConfigurationParameters;
  readonly info: InfoApi;
  readonly user: UserApi;
  readonly book: BookApi;
  readonly collection: CollectionApi;
  readonly internal: InternalApi;
};

const rentedCores = new Gauge({
  name: "api_rented_cores",
  help: "Currently rented API clients.",
});

const requestCount = new Counter({
  name: "api_requests",
  help: "Number of API requests.",
});

const responseErrorCount = new Counter({
  name: "api_response_errors",
  help: "Number of API error responses.",
});

const cores: ApiClientCore[] = [];

function rentCore(token: string): ApiClientCore {
  let core = cores.pop();

  if (!core) {
    const cfg: ConfigurationParameters = {
      basePath: config.get<string>("api.baseUrl") || BASE_PATH,
      fetchApi: fetch,
      middleware: [
        {
          pre: async (): Promise<void> => {
            requestCount.inc();
          },
          post: async (context): Promise<void> => {
            const { response } = context;

            if (!response.ok) {
              responseErrorCount.inc();

              throw Error((await response.json())?.message || response.statusText);
            }
          },
        },
      ],
    };
    const cfg2 = new Configuration(cfg);

    core = {
      config: cfg,
      info: new InfoApi(cfg2),
      user: new UserApi(cfg2),
      book: new BookApi(cfg2),
      collection: new CollectionApi(cfg2),
      internal: new InternalApi(cfg2),
    };
  }

  core.config.accessToken = token;

  rentedCores.inc();
  return core;
}

function returnCore(core: ApiClientCore): void {
  cores.push(core);

  rentedCores.dec();
}

/** Represents an nhitomi API client. */
export class ApiClient implements ApiClientCore {
  _core?: ApiClientCore;

  get core(): ApiClientCore {
    const core = this._core;

    if (core) return core;
    throw Error("API client was destroyed.");
  }

  get config(): ConfigurationParameters {
    return this.core.config;
  }

  get info(): InfoApi {
    return this.core.info;
  }

  get user(): UserApi {
    return this.core.user;
  }

  get book(): BookApi {
    return this.core.book;
  }

  get collection(): CollectionApi {
    return this.core.collection;
  }

  get internal(): InternalApi {
    return this.core.internal;
  }

  constructor(token: string) {
    this._core = rentCore(token);
  }

  /** Destroys this API client, making it unusable. */
  destroy(): void {
    if (this._core) returnCore(this._core);

    this._core = undefined;
  }
}

class BotApiClient extends ApiClient {
  constructor() {
    super(config.get<string>("api.token"));
  }

  /** Bot API information. */
  currentInfo!: GetInfoAuthenticatedResponse;

  /** URL to make API requests to. */
  get baseUrl(): string {
    return this.config.basePath || "";
  }

  /** URL to use to format links. */
  get publicUrl(): string {
    return this.currentInfo.publicUrl;
  }

  /** Formats a link to an API route using publicUrl. */
  getApiLink(path: string): string {
    return new URL(path, `${this.publicUrl}/api/v1/`).href;
  }

  /** Formats a link to a frontend route using publicUrl. */
  getWebLink(path: string): string {
    return new URL(path, this.publicUrl).href;
  }

  async initialize(): Promise<void> {
    const info = await this.info.getInfoAuthenticated();
    const refresh = !!this.currentInfo;

    this.currentInfo = info;

    if (!refresh) console.log("api initialized", this.currentInfo);
  }

  destroy(): void {
    // prevent bot client destruction
  }
}

/** Global nhitomi API client authenticated as bot user. */
export const Api = new BotApiClient();

// refresh client info frequently
setInterval(() => Api.initialize(), 5 * 60 * 1000);
