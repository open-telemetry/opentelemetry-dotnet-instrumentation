# Info

The files here were copied from
<https://github.com/dotnet/runtime/tree/v7.0.0/src/coreclr>.

This is to allow using the runtime's Platform Adaptation Layer.

Manual changes:
in `\inc\corhlpr.cpp:L143`,
assert surrounded by `_DEBUG` conditional compilation

```cpp
+#ifdef _DEBUG
     assert(&origBuff[size] == outBuff);
+#endif
```

in `pal\sal.h:2610` commented ouf `#define __valid`

```cpp
    // #define __valid
```

in `pal/inc/rt/specstrings.h:317` commented out the legacy `#define __bound`

```cpp
    // #define __bound                             __inner_bound
```

On non-Microsoft compilers, `__inner_bound` expands to nothing. Leaving this
macro enabled removes libstdc++ identifiers named `__bound` during
preprocessing and breaks the Linux C++20 build.
