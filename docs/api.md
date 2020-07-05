# API

nhitomi provides an [HTTP API](https://nhitomi.chiya.dev/api/v1) that can be used to automate access to the database. An OpenAPI 3.0 specification is available which can be used to generate the API client in any language using [openapi-generator](https://github.com/OpenAPITools/openapi-generator).

## Authentication

Most endpoints require bearer token authentication. The token can be obtained from `localStorage.token` (quotes excluded) after signing into [nhitomi](https://nhitomi.chiya.dev).

<TokenDisplay>Your API token is: `{token}`</TokenDisplay>

## Limits

Certain permissions may be required to access some endpoints. If you lack the permissions, a `403 Forbidden` status will be returned. These permissions are documented in the API documentation.

Depending on the endpoint, rate limits may be imposed based on the authenticated user or the requester's IP address. If you exceed this rate limit, a `429 Too Many Requests` status will be returned with a [Retry-After](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Retry-After) header. Repeat offences may result in a temporary account restriction or an IP ban.
