//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the TestedPackageVersionsGenerator tool. To safely
//     modify this file, edit PackageVersionDefinitions.cs and
//     re-run the TestedPackageVersionsGenerator project in Visual Studio.

//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated. 
// </auto-generated>
//------------------------------------------------------------------------------

namespace IntegrationTests;

public static class TestPackageVersions
{
    public static readonly IReadOnlyCollection<object[]> StackExchangeRedis = new List<object[]>
    {
#if DEFAULT_TEST_PACKAGE_VERSIONS
        new object[] { string.Empty }
#else
        new object[] { "2.0.495" }
        new object[] { "2.1.50" }
        new object[] { "2.5.61" }
        new object[] { "2.6.66" }
        new object[] { "2.6.90" }
#endif
    };
}
