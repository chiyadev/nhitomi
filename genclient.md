# Genclient

nhitomi provides an OpenAPI 3.0 specification which can be used to generate an HTTP API client in any language using [openapi-generator](https://github.com/OpenAPITools/openapi-generator).

### Prerequisites

- [Node.js](https://nodejs.org/en/)
- [Java Runtime 8+](https://www.java.com/en/download/)

### Setup

[nhitomi-web](nhitomi-web) and [nhitomi-discord](nhitomi-discord) subprojects leverage the generated API client. These projects must be initialized using the `genclient` npm script before the build step.

```shell
$ npm i
$ npm run genclient
```

`genclient` will by default generate an API client for the latest deployed version of nhitomi, which is available [here](https://nhitomi.chiya.dev/api/v1).

`genclient` optionally accepts a path or URL to the API specification file. This can be useful when you are running your own instance of nhitomi-idx and making changes to the specification.

```shell
$ npm run genclient [local | <spec path>]
```

- `local` is a shorthand for `localhost:5000`.
