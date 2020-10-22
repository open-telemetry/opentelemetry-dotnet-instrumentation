This directory and its subdirectories contains source files that are used across several projects.

One of the reasons for doing it is to share the implementation across projects for APIs that should
not be public and therefore cannot be grouped into a separate shared assembly.

The subdirectories here carry the name of the respective namespaces.
E.g. all classes in a namespace `OpenTelemetry.AutoInstrumentation.ContextPropagation` must be
placed in a subdirectory with the same name.
