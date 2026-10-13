#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/clr_helpers.h"
#include "test_helpers.h"

#ifdef _WIN32
#include <Windows.h>
#endif
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
    std::set<std::wstring> expected = {L"ConfiguredTaskAwaiter",
                                       L"DebuggingModes",
                                       L"Enumerator",
                                       L"System.ArgumentNullException",
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
                                       L"System.Runtime.CompilerServices.ConfiguredTaskAwaitable",
                                       L"System.Runtime.CompilerServices.CompilationRelaxationsAttribute",
                                       L"System.Runtime.CompilerServices.CompilerGeneratedAttribute",
                                       L"System.Runtime.CompilerServices.IAsyncStateMachine",
                                       L"System.Runtime.CompilerServices.RuntimeCompatibilityAttribute",
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

TEST_F(CLRHelperTest, GetsRuntimeAsyncVoidResultType)
{
    mdTypeRef task_type_ref = mdTypeRefNil;
    for (auto& type_ref : EnumTypeRefs(metadata_import_))
    {
        if (GetTypeInfo(metadata_import_, type_ref).name == WStr("System.Threading.Tasks.Task"))
        {
            task_type_ref = type_ref;
            break;
        }
    }
    ASSERT_NE(task_type_ref, mdTypeRefNil);

    COR_SIGNATURE compressed_task_token[4]{};
    const auto    token_length = CorSigCompressToken(task_type_ref, compressed_task_token);
    ASSERT_NE(token_length, static_cast<ULONG>(-1));

    std::vector<COR_SIGNATURE> runtime_async_return_signature = {ELEMENT_TYPE_CLASS};
    runtime_async_return_signature.insert(runtime_async_return_signature.end(), compressed_task_token,
                                          compressed_task_token + token_length);

    TypeSignature runtime_async_return_type = {0, static_cast<ULONG>(runtime_async_return_signature.size()),
                                               runtime_async_return_signature.data()};
    TypeSignature result_type{};
    ASSERT_TRUE(TryGetRuntimeAsyncResultType(runtime_async_return_type, metadata_import_, &result_type));

    PCCOR_SIGNATURE result_signature = nullptr;
    ASSERT_EQ(result_type.GetSignature(result_signature), 1);
    EXPECT_EQ(*result_signature, ELEMENT_TYPE_VOID);
}

TEST_F(CLRHelperTest, GetsRuntimeAsyncMultidimensionalArrayResultType)
{
    mdTypeRef task_type_ref = mdTypeRefNil;
    for (auto& type_ref : EnumTypeRefs(metadata_import_))
    {
        if (GetTypeInfo(metadata_import_, type_ref).name == WStr("System.Threading.Tasks.Task`1"))
        {
            task_type_ref = type_ref;
            break;
        }
    }
    ASSERT_NE(task_type_ref, mdTypeRefNil);

    COR_SIGNATURE compressed_task_token[4]{};
    const auto    token_length = CorSigCompressToken(task_type_ref, compressed_task_token);
    ASSERT_NE(token_length, static_cast<ULONG>(-1));

    std::vector<COR_SIGNATURE> runtime_async_return_signature = {ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS};
    runtime_async_return_signature.insert(runtime_async_return_signature.end(), compressed_task_token,
                                          compressed_task_token + token_length);
    runtime_async_return_signature.push_back(1); // generic argument count

    const std::vector<COR_SIGNATURE> expected_result_signature = {ELEMENT_TYPE_ARRAY, ELEMENT_TYPE_I4, 2, 0,
                                                                  0}; // int[,] with rank 2, no sizes or lower bounds
    runtime_async_return_signature.insert(runtime_async_return_signature.end(), expected_result_signature.begin(),
                                          expected_result_signature.end());

    TypeSignature runtime_async_return_type = {0, static_cast<ULONG>(runtime_async_return_signature.size()),
                                               runtime_async_return_signature.data()};
    TypeSignature result_type{};
    ASSERT_TRUE(TryGetRuntimeAsyncResultType(runtime_async_return_type, metadata_import_, &result_type));

    PCCOR_SIGNATURE result_signature = nullptr;
    const auto      result_length    = result_type.GetSignature(result_signature);
    ASSERT_EQ(result_length, expected_result_signature.size());
    EXPECT_EQ(std::vector<COR_SIGNATURE>(result_signature, result_signature + result_length),
              expected_result_signature);
}

TEST_F(CLRHelperTest, RejectsNonRuntimeAsyncGenericReturnType)
{
    mdTypeRef tuple_type_ref = mdTypeRefNil;
    for (auto& type_ref : EnumTypeRefs(metadata_import_))
    {
        if (GetTypeInfo(metadata_import_, type_ref).name == WStr("System.Tuple`2"))
        {
            tuple_type_ref = type_ref;
            break;
        }
    }
    ASSERT_NE(tuple_type_ref, mdTypeRefNil);

    COR_SIGNATURE compressed_tuple_token[4]{};
    const auto    token_length = CorSigCompressToken(tuple_type_ref, compressed_tuple_token);
    ASSERT_NE(token_length, static_cast<ULONG>(-1));

    std::vector<COR_SIGNATURE> return_signature = {ELEMENT_TYPE_GENERICINST, ELEMENT_TYPE_CLASS};
    return_signature.insert(return_signature.end(), compressed_tuple_token, compressed_tuple_token + token_length);
    return_signature.push_back(1);
    return_signature.push_back(ELEMENT_TYPE_I4);

    TypeSignature return_type = {0, static_cast<ULONG>(return_signature.size()), return_signature.data()};
    TypeSignature result_type{};
    EXPECT_FALSE(TryGetRuntimeAsyncResultType(return_type, metadata_import_, &result_type));
}

TEST(CLRHelperSignatureTest, ParsesMultidimensionalArrayInGenericReturnType)
{
    // Method signature returning Task<int[,]> with no parameters. The TypeRefEncoded value 5 is
    // TypeRef row 1; metadata resolution is not needed by FunctionMethodSignature::TryParse.
    const COR_SIGNATURE signature_bytes[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT,
                                             0,
                                             ELEMENT_TYPE_GENERICINST,
                                             ELEMENT_TYPE_CLASS,
                                             5,
                                             1,
                                             ELEMENT_TYPE_ARRAY,
                                             ELEMENT_TYPE_I4,
                                             2,
                                             0,
                                             0};

    FunctionMethodSignature signature(signature_bytes, static_cast<unsigned>(sizeof(signature_bytes)));
    ASSERT_EQ(signature.TryParse(), S_OK);

    PCCOR_SIGNATURE return_signature = nullptr;
    const auto      return_length    = signature.GetReturnValue().GetSignature(return_signature);
    ASSERT_EQ(return_length, sizeof(signature_bytes) - 2);
    EXPECT_EQ(std::vector<COR_SIGNATURE>(return_signature, return_signature + return_length),
              std::vector<COR_SIGNATURE>(signature_bytes + 2, signature_bytes + sizeof(signature_bytes)));
}

#ifdef _WIN32
// Memory-safety regression tests for the managed-metadata signature parser used by
// FunctionMethodSignature::TryParse. ParseRetType/ParseType historically dereferenced *pbCur
// before the pbCur < pbEnd bounds check, reading one byte past a truncated signature blob.
// The signature bytes are placed at the end of a committed page backed by a PAGE_NOACCESS
// guard page, so any over-read faults deterministically (caught via SEH).
namespace
{

class GuardedSignatureBuffer
{
public:
    explicit GuardedSignatureBuffer(const unsigned char* bytes, size_t size)
    {
        SYSTEM_INFO si{};
        GetSystemInfo(&si);
        const size_t page = si.dwPageSize;

        // The signature must fit within the committed page so that its final byte abuts the
        // guard page; a larger request cannot be represented by this helper.
        if (size == 0 || size > page)
        {
            return;
        }

        auto* base = static_cast<unsigned char*>(VirtualAlloc(nullptr, page * 2, MEM_RESERVE, PAGE_NOACCESS));
        if (base == nullptr)
        {
            return;
        }

        if (VirtualAlloc(base, page, MEM_COMMIT, PAGE_READWRITE) == nullptr)
        {
            VirtualFree(base, 0, MEM_RELEASE);
            return;
        }

        // Only publish base_/data_ once both allocations succeeded; on failure data() stays null
        // and the test asserts on it rather than computing an invalid pointer / copying into it.
        base_ = base;
        data_ = base_ + page - size; // last byte abuts the guard page
        memcpy(data_, bytes, size);
    }

    ~GuardedSignatureBuffer()
    {
        if (base_ != nullptr)
        {
            VirtualFree(base_, 0, MEM_RELEASE);
        }
    }

    GuardedSignatureBuffer(const GuardedSignatureBuffer&)            = delete;
    GuardedSignatureBuffer& operator=(const GuardedSignatureBuffer&) = delete;

    const unsigned char* data() const
    {
        return data_;
    }

private:
    unsigned char* base_ = nullptr;
    unsigned char* data_ = nullptr;
};

// SEH wrapper - must not own any C++ objects requiring unwinding.
bool TryParseAccessesGuardPage(FunctionMethodSignature& signature, HRESULT& hrOut)
{
    __try
    {
        hrOut = signature.TryParse();
        return false;
    }
    __except (GetExceptionCode() == EXCEPTION_ACCESS_VIOLATION ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH)
    {
        return true;
    }
}

} // namespace

TEST(CLRHelperSignatureSafetyTest, TruncatedSignatureBeforeReturnTypeDoesNotReadPastEnd)
{
    // [callconv = IMAGE_CEE_CS_CALLCONV_DEFAULT (0x00)] [param count = 1] and nothing else:
    // after consuming both bytes pbCur == pbEnd, so ParseRetType must not dereference.
    const unsigned char sig[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT, 0x01};

    GuardedSignatureBuffer buffer(sig, sizeof(sig));
    ASSERT_NE(buffer.data(), nullptr) << "Failed to set up the guard-page buffer for the test.";
    FunctionMethodSignature signature(buffer.data(), static_cast<unsigned>(sizeof(sig)));

    HRESULT    hr      = E_UNEXPECTED;
    const bool faulted = TryParseAccessesGuardPage(signature, hr);

    ASSERT_FALSE(faulted) << "ParseRetType read one byte past the end of the signature buffer.";
    ASSERT_EQ(hr, E_FAIL) << "A truncated signature must be rejected without reading past the buffer.";
}

TEST(CLRHelperSignatureSafetyTest, TrailingSzArrayElementDoesNotReadPastEnd)
{
    // [callconv 0x00] [param count 1] [ret type = ELEMENT_TYPE_SZARRAY (0x1D)] with no element type.
    const unsigned char sig[] = {IMAGE_CEE_CS_CALLCONV_DEFAULT, 0x01, ELEMENT_TYPE_SZARRAY};

    GuardedSignatureBuffer buffer(sig, sizeof(sig));
    ASSERT_NE(buffer.data(), nullptr) << "Failed to set up the guard-page buffer for the test.";
    FunctionMethodSignature signature(buffer.data(), static_cast<unsigned>(sizeof(sig)));

    HRESULT    hr      = E_UNEXPECTED;
    const bool faulted = TryParseAccessesGuardPage(signature, hr);

    ASSERT_FALSE(faulted) << "ParseType (SZARRAY) read one byte past the end of the signature buffer.";
    ASSERT_EQ(hr, E_FAIL) << "A signature ending in SZARRAY must be rejected without reading past the buffer.";
}
#endif
