# nhitomi-web

This is nhitomi browser frontend using Preact with Typescript.

### Prerequisites

- [Node.js and npm](https://nodejs.org/en/)
- [Java 8+ Runtime](https://www.java.com/en/download/) (for `genclient`)

## Setup

```shell
# Dependency installation
$ npm install

# API client generation
$ npm run genclient
```

## API Client

nhitomi-web uses a Typescript API client automatically generated using [openapi-generator](https://github.com/OpenAPITools/openapi-generator). When the API specification changes, you should update your API client.

```shell
$ npm run genclient
```

If you are running nhitomi-idx together with nhitomi-web, you can also update the client using locally generated specification, assuming the server is running on port 5000.

```shell
$ npm run genclient:local
```
