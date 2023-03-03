# Using the OpenTelemetry.AutoInstrumentation NuGet packages

⚠️ Currently, NuGet packages are only available as CI artifacts.
When following these instructions, ensure that the packages are downloaded
and that the target project is either using a `nuget.config` file configured to use
the downloaded packages, for example the
[`nuget.config` used by the NuGet packages test applications](../../test/test-applications/nuget-packages/nuget.config),
or the packages are added to the project by specifying the `--source` parameter
when running [`dotnet add package` command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-add-package).

## When to use the NuGet packages

Use the NuGet packages in the following scenarios:

1. Simplify deployment. For example, a container running a single application.
1. Support instrumentation of [`self-contained`](https://learn.microsoft.com/en-us/dotnet/core/deploying/#publish-self-contained)
  applications.
1. Facilitate developer experimentation with automatic instrumentation through NuGet packages.

## Limitations

While the NuGet packages are a convenient way to deploy the automatic
instrumentation. It isn't possible or desirable to use them in all cases.
The most common reasons to not use, or limit, the usage of the NuGet
packages are:

1. It is not possible to add the package to the application project, e.g.:
the applications is produced by a third party that can't or is unwilling
to add the package.
1. Reduce disk usage, or the size of a virtual machine, when multiple applications
to be instrumented are installed in a single machine. In this case a single
deployment can be used by all .NET applications running on the machine.
1. A legacy application that can't be migrated to the [SDK-style project](https://learn.microsoft.com/en-us/nuget/resources/check-project-format#check-the-project-format).

## Using the NuGet packages

To automatically instrument your application with OpenTelemetry .NET add
the `OpenTelemetry.AutoInstrumentation` package to your project:

```terminal
dotnet add [<PROJECT>] package OpenTelemetry.AutoInstrumentation --source <AUTO_INSTRUMENTATION_PACKAGES_PATH> --prerelease
```

To ship the correct native runtime components with your .NET application you
must specify a [Runtime Identifier (RID)](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog)
to build the application via `dotnet build` or `dotnet publish`. This may
require choosing between shipping a
[_self-contained_ or _framework-dependent_](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
application. Either choice will work with the automatic instrumentation.

On the output folder of the build you will find a script that can be used to launch
the application with the OpenTelemetry .NET automatic instrumentation.

- On Windows: use `instrument.cmd <application_executable>`
- On Unix: use `instrument.sh <application_executable>`

If the application is launched using the `dotnet` CLI just launch it using the script:

- On Windows: use `instrument.cmd dotnet <application>`
- On Unix: use `instrument.sh dotnet <application>`

The script will forward any command-line parameters that you need to pass to the
application.
