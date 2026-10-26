# image-server
ImageFlow-based image server that can read presets configurations from appsettings.json at startup.

Minimal configuration settings available:
* CacheDirectory: base directory to host the cache folder
* CacheMaxAge: cache header
* CacheSize: file-system cache size
* SignatureKey: when not empty, it is possible to give commands on the query-string as long as the request is signed
* Presets: array of presets, each defined by a name and an array of commands

## SGAuthorize (legacy Forms Auth)

`RequestAuthorizeMiddleware` ports the .NET Framework `SGAuthorize` HttpModule. It runs before Imageflow/static files.

Authorization is **optional**: if `SGAuthorize:Filter` is empty or missing, services and middleware are not activated and requests are not evaluated.

Configure the `SGAuthorize` section in `appsettings.json` (keys map 1:1 to the old `SGAuthorize*` appSettings, plus machineKey values for cookie decryption):

| Setting | Legacy key | Notes |
|---|---|---|
| Connection | SGAuthorizeConnection | SQL connection string (prefer secrets/env in prod) |
| Procedure | SGAuthorizeProcedure | Default `sg_AuthorizeRequest` |
| RedirectTo | SGAuthorizeRedirectTo | Login redirect |
| RedirectToStatusCode | SGAuthorizeRedirectToStatusCode | Default `302` |
| RedirectTo403 | SGAuthorizeRedirectTo403 | Authenticated-deny redirect |
| RedirectToQS | SGAuthorizeRedirectToQS | Return-url query key (default `to`) |
| Filter | SGAuthorizeFilter | Regex on full request URL; empty disables auth |
| ConnectionAttempts | SGAuthorizeConnectionAttempts | Default `5` |
| ConnectionAttemptsSleep | SGAuthorizeConnectionAttemptsSleep | Default `500` ms |
| TicketToken | SGAuthorizeTicketToken | 1-based UserData index (default `9`) |
| TicketAdminToken | SGAuthorizeTicketAdminToken | 1-based UserData index (default `1`) |
| TicketTokenSeparator | SGAuthorizeTicketTokenSeparator | Default `\|` |
| ValidateAnonymousUsers | SGAuthorizeValidateAnonymousUsers | Default `false` |
| CacheSeconds | SGAuthorizeCacheSeconds | Default `30`; `0` disables |
| CookieName | (Forms auth) | Default `.ASPXAUTH` |
| ValidationKey / DecryptionKey | machineKey | Hex keys from the legacy site |
| ShaVersion | machineKey validation | `Sha1`, `Sha256`, or `Sha512` |
| CompatibilityMode | machineKey compatibilityMode | `Framework45` or `Framework20SP2` |
