# Instrumentation Stability

When describing instrumentation stability there are two main ways to define stability.

1. The insttrumentation API surface area
2. The semantic convention stability

See the [OpenTelemetry stability proposal announcement](https://opentelemetry.io/blog/2025/stability-proposal-announcement/) for more information on this subject.
The main takeaway from the proposal is that instrumentation stability is something that can be declared independently from the semantic convention stability.
However, end users care more about whether or not telemetry will change when they perform an upgrade. That experience more closely aligns more with semantic
convention stability. Until more definition is provided on how to communicate semantic convention compliance, the definition of stable will be based on the
instrumentation API stability.

## Instrumentation types and stability

Auto instrumentation has the ability to manage multiple forms of instrumentation.

1. Natively instrumented code
2. Instrumentation libraries
3. Byte-code instrumentation
4. No-code instrumentation

Each of these instrumentation types have their own maintainers that may be different than the maintainers of auto instrumentations.

### Natively instrumented code

An example of this type of instrumentation is a library such as `Npgsql`, which is used by an end user, include OpenTelemetry instrumentation directly in its
code. An end user, or auto instrumentation does not need to bring in additional dependencies to make the instrumentation available. The maintenance of this
code is outside of the scope of auto instrumentation. The ability to control which version of the instrumentation is used, is also outside of the control
of auto instrumentation. For example, if an end user is using `Npgsql` version 6 in their application, they will be tied to the semantic conventions
produced by that version of the library. Auto instrumentation will should not upgrade or download the `Npgsql` version just to enforce that a specific 
semantic convention version is used between minor releases of auto instrumentation.

Auto instrumentation does not need to test for semantic convention compliance for natively instrumented code. Semantic convention compliance is defined 
separately from instrumentation stability, and auto instrumentation has no control over which version of a library is used to attempt to enforce a consistent
semantic convention compliance target. As a result, a thorough testing of semantic convention compliance would just bring addtional toil to the maintenance
of auto instrumentation.

### Instrumentation libraries

An example of this type of instrumentation is `OpenTelemetry.Instrumentation.Quartz` where the Quartz library used by an end user does not produce 
OpenTelemetry compatible telemetry, and a seperate library must be added to the application and configured (either by an end user or auto instrumentation)
in order to produce OpenTelemetry compatible telemetry. These libraries are maintained separately from auto instrumentation, but auto instrumentation
has some capabilities to control which version of the instrumentation library is loaded into the application.

Auto instrumentation does not need to test for semantic convention compliance in these libraries. Many of these libraries are either translating existing
telemetry into an OpenTelemetry compliant form, or are leveraging public extensibility options in the library to generate OpenTelemetry compliant
telemetry. In either case, the stability of this instrumentation for auto instrumentation is related to the instrumentation API stability and what
auto instrumentation needs to do to enable the telemetry. None of those factors require testing for semantic convention compliance.

### Byte-code instrumentation

An example of this instrumentation is the Kafka instrumentation provided by auto instrumentation. This type of instrumentation is owned and maintained by
auto instrumentation. As a result, auto instrumentation is responsible for both the instrumentation API stability and semantic convention compliance.
Even through auto instrumentation is responsible for both aspecs, the instrumentation API stability is what is needed to declare the instrumentation 
stable. The semantic convention compliance will need to be declared separately.

Auto instrumentation *MUST* test for semantic convention compliance. This is the primary mechanism we can use to determine if our instrumentation will
continue to work and provide the expected data across multiple versions of the library.

### No-code instrumentation

An example of this instrumentation is any custom instrumentation that an end user adds to their OpenTelemetry declarative configuration file.
OpenTelemetry can provide no guaraties about what custom code is doing within that custom instrumentation. The only thing that we can do is to
provide a stable API surface area for that instrumentation. The stability of No-code instrumentation is not based on the telemetry produced
by No-code instrumentation. It is tied to the stability of the APIs that can be used to define No-code instrumentation. Auto instrumentation
does not ship with any available No-code instrumentations (because they are reserved for custom instrumentation where the end user cannot 
modify the original source code), so there is no need to declare any No-code instrumentations as stable.

Auto-instrumentation *MUST* test the No-code instrumentation APIs, but do not test for semantic convention compliance.

