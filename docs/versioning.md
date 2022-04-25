# Versioning

This document defines the stability guarantees offered by
the OpenTelemetry .NET Automatic Instrumentation,
along with the rules and procedures for meeting those guarantees.

This project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
with one exception described [here](#compatibility-with-opentelemetry-net-and-other-dependencies).

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

See [here](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/versioning-and-stability.md#semantic-conventions-stability).

## Major version bump

Major version bumps occurs when there is a breaking change to a stable interface
or some functionality has been removed.

* A stable configuration has been renamed.
* A stable functionality has been removed.

## Minor version bump

Most changes result in a minor version bump.

* New backward-compatible functionality added.
* Experimental functionality become stable.
* Breaking changes to experimental functionalities.
* New experimental functionality is added.
* Deprecation of a stable functionality.
* Dependencies bump.

### Patch version bump

Most changes result in a patch version bump.

Patch versions make no changes which would require recompilation
or potentially break application code.
The following are examples of patch fixes.

* Bug fixes which do not require minor version bump per rules above.
* Security fixes.

## Version numbers before 1.0.0

We simply convert to Semantic Versioning scheme from
`MAJOR.MINOR.PATCH` to `0.MAJOR.MINOR-beta.PATCH`.
