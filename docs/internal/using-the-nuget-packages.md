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
1. Facilitate developer experimentation with automatic instrumentation through
  NuGet packages.

## Limitations

While NuGet packages are a convenient way to deploy automatic
instrumentation, they can't be used in all cases. The most common
reasons for not using NuGet packages include the following:

1. You can't add the package to the application project. For example,
the application is from a third party that can't add the package.
1. Reduce disk usage, or the size of a virtual machine, when multiple applications
to be instrumented are installed in a single machine. In this case you can use
a single deployment for all .NET applications running on the machine.
1. A legacy application that can't be migrated to the [SDK-style project](https://learn.microsoft.com/en-us/nuget/resources/check-project-format#check-the-project-format).

## Using the NuGet packages

To automatically instrument your application with OpenTelemetry .NET add
the `OpenTelemetry.AutoInstrumentation` package to your project:

```terminal
dotnet add [<PROJECT>] package OpenTelemetry.AutoInstrumentation --source <PATH_TO_AUTO_INSTRUMENTATION_PACKAGES> --prerelease
```

To distribute the appropriate native runtime components with your .NET application,
specify a [Runtime Identifier (RID)](https://learn.microsoft.com/en-us/dotnet/core/rid-catalog)
to build the application using `dotnet build` or `dotnet publish`. This might
require choosing between distributing a
[_self-contained_ or a _framework-dependent_](https://learn.microsoft.com/en-us/dotnet/core/deploying/)
application. Both types are compatible with automatic instrumentation.

Use the script in the output folder of the build to launch the
application with automatic instrumentation activated.

- On Windows, use `instrument.cmd <application_executable>`
- On Linux or Unix, use `instrument.sh <application_executable>`

If you launch the application using the `dotnet` CLI, add `dotnet` after the script.

- On Windows, use `instrument.cmd dotnet <application>`
- On Linux and Unix, use `instrument.sh dotnet <application>`

The script passes to the application all the command-line parameters you provide.
