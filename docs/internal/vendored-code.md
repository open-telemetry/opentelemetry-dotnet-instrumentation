# Vendored code

To avoid a direct dependency on external packages, some code is vendored.

This means that source code from external repositories is copied into the `/src/OpenTelemetry.AutoInstrumentation/Vendors/{LibName}`
folder.

To make vendored code fully internal, apply the following changes:

1. Prefix all namespaces with `Vendors.` (e.g., change `YamlDotNet` to `Vendors.YamlDotNet`).
2. Update all using directives to reference the vendored namespaces
   (e.g., change `using YamlDotNet` to `using Vendors.YamlDotNet`).
3. Mark whole public contract as internal.
4. Update `src\OpenTelemetry.AutoInstrumentation\Vendors\.editorconfig` to avoid
   issues caused by imported code. Vendored files should not be changed manually.

## Status

### YamlDotNet

`YamlDotNet` is needed for file-based configuration.

* Source: <https://github.com/aaubry/YamlDotNet>
* Version: `16.3.0`
* Content: Only the `YamlDotNet` folder is added to
  `src\OpenTelemetry.AutoInstrumentation\Vendors\YamlDotNet`.
