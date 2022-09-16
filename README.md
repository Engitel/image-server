# image-server
ImageFlow-based image server that can read presets configurations from appsettings.json at startup.

Minimal configuration settings available:
* CacheDirectory: base directory to host the cache folder
* CacheMaxAge: cache header
* CacheSize: file-system cache size
* SignatureKey: when not empty, it is possible to give commands on the query-string as long as the request is signed
* Presets: array of presets, each defined by a name and an array of commands
