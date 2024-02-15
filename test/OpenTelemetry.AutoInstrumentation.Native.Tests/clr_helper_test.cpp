#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/clr_helpers.h"
#include "test_helpers.h"

#include <vector>

using namespace trace;

class CLRHelperTest : public ::CLRHelperTestBase
{
};
TEST_F(CLRHelperTest, EnumeratesTypeDefs)
{
    std::vector<std::wstring> expected_types = {L"Microsoft.CodeAnalysis.EmbeddedAttribute",
                                                L"System.Runtime.CompilerServices.IsReadOnlyAttribute",
                                                L"System.Runtime.CompilerServices.NullableAttribute",
                                                L"System.Runtime.CompilerServices.NullableContextAttribute",
                                                L"System.Runtime.CompilerServices.RefSafetyRulesAttribute",
                                                L"TestApplication.ExampleLibrary.Class1",
                                                L"TestApplication.ExampleLibrary.GenericTests.ComprehensiveCaller`2",
                                                L"TestApplication.ExampleLibrary.GenericTests.GenericTarget`2",
                                                L"TestApplication.ExampleLibrary.GenericTests.PointStruct",
                                                L"TestApplication.ExampleLibrary.GenericTests.StructContainer`1",
                                                L"TestApplication.ExampleLibrary.FakeClient.Biscuit`1",
                                                L"TestApplication.ExampleLibrary.FakeClient.Biscuit",
                                                L"TestApplication.ExampleLibrary.FakeClient.StructBiscuit",
                                                L"TestApplication.ExampleLibrary.FakeClient.DogClient`2",
                                                L"TestApplication.ExampleLibrary.FakeClient.DogTrick`1",
                                                L"TestApplication.ExampleLibrary.FakeClient.DogTrick",
                                                L"<>c",
                                                L"Cookie",
                                                L"Cookie",
                                                L"<StayAndLayDown>d__4`2",
                                                L"Raisin"};

    std::vector<std::wstring> actual_types;

    for (auto& def : EnumTypeDefs(metadata_import_))
    {
        std::wstring name(256, 0);
        DWORD        name_sz = 0;
        DWORD        flags   = 0;
        mdToken      extends = 0;
        auto hr = metadata_import_->GetTypeDefProps(def, name.data(), (DWORD)(name.size()), &name_sz, &flags, &extends);
        ASSERT_TRUE(SUCCEEDED(hr));

        if (name_sz > 0)
        {
            name = name.substr(0, name_sz - 1);
            actual_types.push_back(name);
        }
    }

    EXPECT_EQ(expected_types, actual_types);
}

TEST_F(CLRHelperTest, EnumeratesAssemblyRefs)
{
    std::vector<std::wstring> expected_assemblies = {L"mscorlib"};
    std::vector<std::wstring> actual_assemblies;
    for (auto& ref : EnumAssemblyRefs(assembly_import_))
    {
        auto name = GetReferencedAssemblyMetadata(assembly_import_, ref).name;
        if (!name.empty())
        {
            actual_assemblies.push_back(name);
        }
    }
    EXPECT_EQ(expected_assemblies, actual_assemblies);
}

TEST_F(CLRHelperTest, GetsTypeInfoFromTypeDefs)
{
    std::set<std::wstring> expected = {L"<>c",
                                       L"<StayAndLayDown>d__4`2",
                                       L"Cookie",
                                       L"Microsoft.CodeAnalysis.EmbeddedAttribute",
                                       L"Raisin",
                                       L"System.Runtime.CompilerServices.IsReadOnlyAttribute",
                                       L"System.Runtime.CompilerServices.NullableAttribute",
                                       L"System.Runtime.CompilerServices.NullableContextAttribute",
                                       L"System.Runtime.CompilerServices.RefSafetyRulesAttribute",
                                       L"TestApplication.ExampleLibrary.Class1",
                                       L"TestApplication.ExampleLibrary.FakeClient.Biscuit",
                                       L"TestApplication.ExampleLibrary.FakeClient.Biscuit`1",
                                       L"TestApplication.ExampleLibrary.FakeClient.DogClient`2",
                                       L"TestApplication.ExampleLibrary.FakeClient.DogTrick",
                                       L"TestApplication.ExampleLibrary.FakeClient.DogTrick`1",
                                       L"TestApplication.ExampleLibrary.FakeClient.StructBiscuit",
                                       L"TestApplication.ExampleLibrary.GenericTests.ComprehensiveCaller`2",
                                       L"TestApplication.ExampleLibrary.GenericTests.GenericTarget`2",
                                       L"TestApplication.ExampleLibrary.GenericTests.PointStruct",
                                       L"TestApplication.ExampleLibrary.GenericTests.StructContainer`1"};
    std::set<std::wstring> actual;
    for (auto& type_def : EnumTypeDefs(metadata_import_))
    {
        auto type_info = GetTypeInfo(metadata_import_, type_def);
        if (type_info.IsValid())
        {
            actual.insert(type_info.name);
        }
    }
    EXPECT_EQ(actual, expected);
}

TEST_F(CLRHelperTest, GetsTypeInfoFromTypeRefs)
{
    std::set<std::wstring> expected = {L"DebuggingModes",
                                       L"Enumerator",
                                       L"System.Array",
                                       L"System.Attribute",
                                       L"System.AttributeTargets",
                                       L"System.AttributeUsageAttribute",
                                       L"System.Byte",
                                       L"System.Collections.DictionaryEntry",
                                       L"System.Collections.Generic.Dictionary`2",
                                       L"System.Collections.Generic.IList`1",
                                       L"System.Collections.Generic.List`1",
                                       L"System.Diagnostics.DebuggableAttribute",
#ifdef _DEBUG
                                       L"System.Diagnostics.DebuggerBrowsableAttribute",
                                       L"System.Diagnostics.DebuggerBrowsableState",
#endif
                                       L"System.Diagnostics.DebuggerHiddenAttribute",
#ifdef _DEBUG
                                       L"System.Diagnostics.DebuggerStepThroughAttribute",
#endif
                                       L"System.Exception",
                                       L"System.Func`3",
                                       L"System.Guid",
                                       L"System.Int32",
                                       L"System.Object",
                                       L"System.Reflection.AssemblyCompanyAttribute",
                                       L"System.Reflection.AssemblyConfigurationAttribute",
                                       L"System.Reflection.AssemblyFileVersionAttribute",
                                       L"System.Reflection.AssemblyInformationalVersionAttribute",
                                       L"System.Reflection.AssemblyProductAttribute",
                                       L"System.Reflection.AssemblyTitleAttribute",
                                       L"System.Runtime.CompilerServices.AsyncStateMachineAttribute",
                                       L"System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1",
                                       L"System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                                       L"System.Runtime.CompilerServices.CompilerGeneratedAttribute",
                                       L"System.Runtime.CompilerServices.IAsyncStateMachine",
                                       L"System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
                                       L"System.Runtime.CompilerServices.TaskAwaiter",
                                       L"System.Runtime.CompilerServices.TaskAwaiter`1",
                                       L"System.Runtime.Versioning.TargetFrameworkAttribute",
                                       L"System.RuntimeTypeHandle",
                                       L"System.String",
                                       L"System.Threading.Tasks.Task",
                                       L"System.Threading.Tasks.Task`1",
                                       L"System.Tuple`2",
                                       L"System.Tuple`7",
                                       L"System.Type",
                                       L"System.ValueType"};
    std::set<std::wstring> actual;
    for (auto& type_ref : EnumTypeRefs(metadata_import_))
    {
        auto type_info = GetTypeInfo(metadata_import_, type_ref);
        if (type_info.IsValid())
        {
            actual.insert(type_info.name);
        }
    }
    EXPECT_EQ(expected, actual);
}

TEST_F(CLRHelperTest, GetsTypeInfoFromModuleRefs)
{
    // TODO(cbd): figure out how to create a module ref, for now its empty
    std::set<std::wstring> expected = {};
    std::set<std::wstring> actual;
    for (auto& module_ref : EnumModuleRefs(metadata_import_))
    {
        auto type_info = GetTypeInfo(metadata_import_, module_ref);
        actual.insert(type_info.name);
    }
    EXPECT_EQ(actual, expected);
}

TEST_F(CLRHelperTest, GetsTypeInfoFromMethods)
{
    std::set<std::wstring> expected = {L"<>c",
                                       L"<StayAndLayDown>d__4`2",
                                       L"Cookie",
                                       L"Microsoft.CodeAnalysis.EmbeddedAttribute",
                                       L"Raisin",
                                       L"System.Runtime.CompilerServices.IsReadOnlyAttribute",
                                       L"System.Runtime.CompilerServices.NullableAttribute",
                                       L"System.Runtime.CompilerServices.NullableContextAttribute",
                                       L"System.Runtime.CompilerServices.RefSafetyRulesAttribute",
                                       L"TestApplication.ExampleLibrary.Class1",
                                       L"TestApplication.ExampleLibrary.FakeClient.Biscuit",
                                       L"TestApplication.ExampleLibrary.FakeClient.Biscuit`1",
                                       L"TestApplication.ExampleLibrary.FakeClient.DogClient`2",
                                       L"TestApplication.ExampleLibrary.FakeClient.DogTrick",
                                       L"TestApplication.ExampleLibrary.FakeClient.DogTrick`1",
                                       L"TestApplication.ExampleLibrary.FakeClient.StructBiscuit",
                                       L"TestApplication.ExampleLibrary.GenericTests.ComprehensiveCaller`2",
                                       L"TestApplication.ExampleLibrary.GenericTests.GenericTarget`2",
                                       L"TestApplication.ExampleLibrary.GenericTests.PointStruct",
                                       L"TestApplication.ExampleLibrary.GenericTests.StructContainer`1"};
    std::set<std::wstring> actual;
    for (auto& type_def : EnumTypeDefs(metadata_import_))
    {
        for (auto& method_def : EnumMethods(metadata_import_, type_def))
        {
            auto type_info = GetTypeInfo(metadata_import_, method_def);
            if (type_info.IsValid())
            {
                actual.insert(type_info.name);
            }
        }
    }
    EXPECT_EQ(actual, expected);
}

TEST_F(CLRHelperTest, FindTypeDefsByName)
{
    std::vector<std::wstring> expected_types = {L"TestApplication.ExampleLibrary.Class1",
                                                L"TestApplication.ExampleLibrary.GenericTests.ComprehensiveCaller`2",
                                                L"TestApplication.ExampleLibrary.GenericTests.GenericTarget`2",
                                                L"TestApplication.ExampleLibrary.GenericTests.PointStruct",
                                                L"TestApplication.ExampleLibrary.GenericTests.StructContainer`1",
                                                L"TestApplication.ExampleLibrary.FakeClient.Biscuit`1",
                                                L"TestApplication.ExampleLibrary.FakeClient.Biscuit",
                                                L"TestApplication.ExampleLibrary.FakeClient.DogClient`2",
                                                L"TestApplication.ExampleLibrary.FakeClient.DogTrick`1",
                                                L"TestApplication.ExampleLibrary.FakeClient.DogTrick"};

    for (auto& def : expected_types)
    {
        mdTypeDef typeDef = mdTypeDefNil;
        auto      found   = FindTypeDefByName(def, L"TestApplication.ExampleLibrary", metadata_import_, typeDef);
        EXPECT_TRUE(found) << "Failed type is : " << def << std::endl;
        EXPECT_NE(typeDef, mdTypeDefNil) << "Failed type is : " << def << std::endl;
    }
}

TEST_F(CLRHelperTest, FindNestedTypeDefsByName)
{
    std::vector<std::wstring> expected_types = {L"TestApplication.ExampleLibrary.FakeClient.Biscuit+Cookie",
                                                L"TestApplication.ExampleLibrary.FakeClient.StructBiscuit+Cookie"};

    for (auto& def : expected_types)
    {
        mdTypeDef typeDef = mdTypeDefNil;
        auto      found   = FindTypeDefByName(def, L"TestApplication.ExampleLibrary", metadata_import_, typeDef);
        EXPECT_TRUE(found) << "Failed type is : " << def << std::endl;
        EXPECT_NE(typeDef, mdTypeDefNil) << "Failed type is : " << def << std::endl;
    }
}

TEST_F(CLRHelperTest, DoesNotFindDoubleNestedTypeDefsByName)
{
    std::vector<std::wstring> expected_types = {L"TestApplication.ExampleLibrary.NotARealClass",
                                                L"TestApplication.ExampleLibrary.FakeClient.Biscuit+Cookie+Raisin"};

    for (auto& def : expected_types)
    {
        mdTypeDef typeDef = mdTypeDefNil;
        auto      found   = FindTypeDefByName(def, L"TestApplication.ExampleLibrary", metadata_import_, typeDef);
        EXPECT_FALSE(found) << "Failed type is : " << def << std::endl;
        EXPECT_EQ(typeDef, mdTypeDefNil) << "Failed type is : " << def << std::endl;
    }
}