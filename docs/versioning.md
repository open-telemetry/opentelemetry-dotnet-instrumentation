# Versioning

This document defines the stability guarantees offered by
the OpenTelemetry .NET Automatic Instrumentation,
along with the rules and procedures for meeting those guarantees.

This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
with one exception described under [Compatibility with OpenTelemetry .NET and
other dependencies](#compatibility-with-opentelemetry-net-and-other-dependencies).

## Release candidates

Release candidates are having the `-rc.N` pre-release label.

## Beta releases

Beta (experimental) releases are having the `-beta.N` pre-release label.

## Compatibility with OpenTelemetry .NET and other dependencies

OpenTelemetry .NET Automatic Instrumentation is built on top of
[OpenTelemetry .NET](https://github.com/open-telemetry/opentelemetry-dotnet).
Unfortunately, this may cause dependency version conflicts
if the instrumented application is referencing the same assemblies
as OpenTelemetry .NET Automatic Instrumentation.

The [troubleshooting document](troubleshooting.md) describes
how to resolve such conflicts.

This may be seen as a "breaking change" from the user
perspective, but unfortunately it is currently unavoidable
and bumping a major version during each dependency version bump
would be an overkill.

## Semantic Conventions stability

See [Semantic Conventions page](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md#semantic-conventions-stability).

## Major version bump

There are a few cases when a major bump may occur.

* Breaking change of a stable functionality or configuration.
* Removal of a stable functionality or configuration.

**Exception:** Changes related to instrumentation libraries are handled
the same way as experimental features.

Major versions bumps are avoided.

## Minor version bump

Most changes result in a minor version bump.

* Addition of new functionality, configuration, or instrumentation library.
* Making experimental functionality or configuration stable.
* Backwards compatible change in a stable functionality or configuration.
* Deprecation of a stable functionality.
* Dependency bump which may affect the user.

Changes related to the instrumentation libraries:

* Change (can be breaking) in a supported instrumentation library's
  functionality or configuration.
* Removal or deprecation of an instrumentation library.

Changes related to the experimental features:

* Change (can be breaking) in an experimental functionality or configuration.
* Removal of an experimental functionality or configuration.

### Patch version bump

Patch versions make no changes that would require recompilation
or potentially break application code.
The following are examples of patch fixes.

* Bug fixes that do not require minor version bumps per rules above.
* Security fixes.

## Version numbers before 1.0.0

We simply convert the Semantic Versioning scheme from
`MAJOR.MINOR.PATCH` to `0.MAJOR.MINOR`.

The changes that would bump the patch version
are bumping the minor version instead.
