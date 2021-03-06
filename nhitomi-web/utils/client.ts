import {
  BookApi,
  CollectionApi,
  CollectionInsertPosition,
  Configuration,
  ConfigurationParameters,
  DownloadApi,
  GetInfoAuthenticatedResponse,
  GetInfoResponse,
  InfoApi,
  InternalApi,
  ObjectType,
  SpecialCollection,
  User,
  UserApi,
  ValidationProblemArrayResult,
} from "nhitomi-api";
import { ValidationError } from "./errors";
import node_fetch from "node-fetch";
import { parseCookies } from "nookies";
import { createContext, useContext } from "react";
import { parseConfigs } from "./config";

export class ApiClient {
  readonly httpConfig: ConfigurationParameters = {
    fetchApi: async (input, init) => {
      for (let i = 0; ; i++) {
        if (i) {
          await new Promise((resolve) => setTimeout(resolve, 1000));
        }

        try {
          if (typeof window !== "undefined") {
            return await window.fetch(input, init);
          } else {
            return await (node_fetch as any)(input, { ...init, highWaterMark: 1024 * 1024 * 1024 });
          }
        } catch (e) {
          // could be a network error
          // if connection is offline, retry indefinitely
          if (typeof navigator !== "undefined" && !navigator.onLine) continue;

          // otherwise retry up to three times
          if (i < 3) continue;

          throw e;
        }
      }
    },

    middleware: [
      {
        post: async (context) => {
          const { response } = context;
          if (response.ok) return;

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

function windowApiUrl() {
  if (typeof window === "undefined") {
    return;
  }

  const { protocol, host, hostname } = window.location;

  if (hostname === "localhost") {
    return;
  } else {
    return `${protocol}//${host}/api/v1`;
  }
}

const ChiyaApiUrl = "https://nhitomi.chiya.dev/api/v1";
const PublicApiUrl = process.env.NEXT_PUBLIC_API_PUBLIC || windowApiUrl() || ChiyaApiUrl;
const InternalApiUrl = process.env.NEXT_PUBLIC_API_INTERNAL || PublicApiUrl;

export function createApiClient(token?: string) {
  if (!token) {
    ({ token } = parseConfigs(parseCookies()));
  }

  // server
  if (typeof window === "undefined") {
    return new ApiClient(InternalApiUrl, token);
  }

  // client
  else {
    return new ApiClient(PublicApiUrl, token);
  }
}

export class ClientUtils {
  static async addToSpecialCollection<T extends { id: string }>(
    client: ApiClient,
    user: User,
    type: ObjectType,
    collection: SpecialCollection,
    item: T
  ) {
    let collectionId = user.specialCollections?.[type]?.[collection];

    for (let i = 0; ; i++) {
      try {
        if (collectionId) {
          return await client.collection.addCollectionItems({
            id: collectionId,
            addCollectionItemsRequest: {
              items: [item.id],
              position: CollectionInsertPosition.Start,
            },
          });
        }
      } catch (e) {
        if (i) {
          throw e;
        }
      }

      collectionId = (
        await client.user.getUserSpecialCollection({
          id: user.id,
          type,
          collection,
        })
      ).id;
    }
  }
}

export const ClientInfoContext = createContext<GetInfoResponse | GetInfoAuthenticatedResponse | undefined>(undefined);

export function useClientInfo() {
  return useContext(ClientInfoContext);
}

export function useClientInfoAuth() {
  const value = useClientInfo();
  return value && "user" in value ? value : undefined;
}
