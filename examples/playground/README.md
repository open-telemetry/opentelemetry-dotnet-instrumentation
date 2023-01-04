# Demo

## Description

This is a ASP.NET Core MVC playground application which can be used
to test the local changes.

## Usage

### Prerequisites

Create a local build of the project
and setup the test environment
following the [developing instructions](../../docs/developing.md).

## Playground application

You can use the `run.sh` helper script to build
and run the playground application with auto instrumenation.

```sh
./examples/playground/run.sh
```

| Env var         | Description                                               | Default  |
|-----------------|-----------------------------------------------------------|----------|
| `CONFIGURATION` | Build configuration. Possible values: `Debug`, `Release`. | `Debug`  |
| `DOTNET`        | .NET version. Possible values: `net7.0`, `net6.0`.        | `net7.0` |

The application should be hosted on <http://localhost:5000/`>.
