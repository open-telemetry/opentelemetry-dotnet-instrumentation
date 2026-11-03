#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/clr_helpers.h"

#include <algorithm>
#include <vector>

using namespace trace;

TEST(AssemblyVersionRedirectionTest, CompareToAssemblyVersion_Equal)
{
    ASSERT_TRUE(AssemblyVersionRedirection(1, 2, 3, 4).CompareToAssemblyVersion(ASSEMBLYMETADATA{1, 2, 3, 4}) == 0);
}

TEST(AssemblyVersionRedirectionTest, CompareToAssemblyVersion_Lower)
{
    ASSERT_TRUE(AssemblyVersionRedirection(0, 2, 3, 4).CompareToAssemblyVersion(ASSEMBLYMETADATA{1, 2, 3, 4}) < 0);
    ASSERT_TRUE(AssemblyVersionRedirection(1, 1, 3, 4).CompareToAssemblyVersion(ASSEMBLYMETADATA{1, 2, 3, 4}) < 0);
    ASSERT_TRUE(AssemblyVersionRedirection(1, 2, 2, 4).CompareToAssemblyVersion(ASSEMBLYMETADATA{1, 2, 3, 4}) < 0);
    ASSERT_TRUE(AssemblyVersionRedirection(1, 2, 3, 3).CompareToAssemblyVersion(ASSEMBLYMETADATA{1, 2, 3, 4}) < 0);
}

TEST(AssemblyVersionRedirectionTest, CompareToAssemblyVersion_Higher)
{
    ASSERT_TRUE(AssemblyVersionRedirection(2, 2, 3, 4).CompareToAssemblyVersion(ASSEMBLYMETADATA{1, 2, 3, 4}) > 0);
    ASSERT_TRUE(AssemblyVersionRedirection(1, 3, 3, 4).CompareToAssemblyVersion(ASSEMBLYMETADATA{1, 2, 3, 4}) > 0);
    ASSERT_TRUE(AssemblyVersionRedirection(1, 2, 4, 4).CompareToAssemblyVersion(ASSEMBLYMETADATA{1, 2, 3, 4}) > 0);
    ASSERT_TRUE(AssemblyVersionRedirection(1, 2, 3, 5).CompareToAssemblyVersion(ASSEMBLYMETADATA{1, 2, 3, 4}) > 0);
}

namespace
{
class AssemblyImportStub : public IMetaDataAssemblyImport
{
private:
    ULONG  references_ = 1;
    size_t position_   = 0;

public:
    std::vector<mdAssemblyRef> assembly_refs;
    std::vector<mdAssemblyRef> inspected_refs;
    ULONG                      enum_calls    = 0;
    ULONG                      failure_call  = 0;
    ULONG                      close_calls   = 0;
    HCORENUM                   closed_handle = nullptr;
    HRESULT                    empty_result  = S_FALSE;

    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID iid, void** result) override
    {
        if (result == nullptr)
        {
            return E_POINTER;
        }
        *result = nullptr;
        if (iid != IID_IUnknown && iid != IID_IMetaDataAssemblyImport)
        {
            return E_NOINTERFACE;
        }
        *result = static_cast<IMetaDataAssemblyImport*>(this);
        AddRef();
        return S_OK;
    }

    ULONG STDMETHODCALLTYPE AddRef() override
    {
        return ++references_;
    }

    ULONG STDMETHODCALLTYPE Release() override
    {
        const auto remaining = --references_;
        if (remaining == 0)
        {
            delete this;
        }
        return remaining;
    }

    HRESULT STDMETHODCALLTYPE EnumAssemblyRefs(HCORENUM* handle, mdAssemblyRef refs[], ULONG max, ULONG* count) override
    {
        *handle = this;
        ++enum_calls;
        if (enum_calls == failure_call)
        {
            if (enum_calls == 1)
            {
                // Output values are undefined on failure and must not be consumed.
                refs[0] = TokenFromRid(1, mdtAssemblyRef);
                *count  = 1;
            }
            // On a later failure, leave the previous batch and count untouched.
            return E_FAIL;
        }
        if (failure_call != 0 && enum_calls > failure_call)
        {
            // Keep a broken caller bounded so the regression test fails without hanging.
            *count = 0;
            return S_FALSE;
        }

        *count = 0;
        while (position_ < assembly_refs.size() && *count < max)
        {
            refs[(*count)++] = assembly_refs[position_++];
        }
        if (*count != 0)
        {
            return S_OK;
        }
        return enum_calls == 1 ? empty_result : S_FALSE;
    }

    void STDMETHODCALLTYPE CloseEnum(HCORENUM handle) override
    {
        ++close_calls;
        closed_handle = handle;
    }

    HRESULT STDMETHODCALLTYPE GetAssemblyRefProps(mdAssemblyRef     assembly_ref,
                                                  const void**      public_key,
                                                  ULONG*            public_key_size,
                                                  LPWSTR            name,
                                                  ULONG             name_capacity,
                                                  ULONG*            name_length,
                                                  ASSEMBLYMETADATA* metadata,
                                                  const void**      hash,
                                                  ULONG*            hash_size,
                                                  DWORD*            flags) override
    {
        const auto assembly_name = WStr("Unmapped.Assembly");
        const auto length        = static_cast<ULONG>(std::char_traits<WCHAR>::length(assembly_name) + 1);
        if (name_capacity < length)
        {
            return E_INVALIDARG;
        }
        inspected_refs.push_back(assembly_ref);
        std::copy_n(assembly_name, length, name);
        *name_length     = length;
        *public_key      = nullptr;
        *public_key_size = 0;
        *metadata        = {};
        *hash            = nullptr;
        *hash_size       = 0;
        *flags           = 0;
        return S_OK;
    }

    HRESULT STDMETHODCALLTYPE GetAssemblyProps(
        mdAssembly, const void**, ULONG*, ULONG*, LPWSTR, ULONG, ULONG*, ASSEMBLYMETADATA*, DWORD*) override
    {
        return E_NOTIMPL;
    }
    HRESULT STDMETHODCALLTYPE GetFileProps(mdFile, LPWSTR, ULONG, ULONG*, const void**, ULONG*, DWORD*) override
    {
        return E_NOTIMPL;
    }
    HRESULT STDMETHODCALLTYPE
    GetExportedTypeProps(mdExportedType, LPWSTR, ULONG, ULONG*, mdToken*, mdTypeDef*, DWORD*) override
    {
        return E_NOTIMPL;
    }
    HRESULT STDMETHODCALLTYPE
    GetManifestResourceProps(mdManifestResource, LPWSTR, ULONG, ULONG*, mdToken*, DWORD*, DWORD*) override
    {
        return E_NOTIMPL;
    }
    HRESULT STDMETHODCALLTYPE EnumFiles(HCORENUM*, mdFile[], ULONG, ULONG*) override
    {
        return E_NOTIMPL;
    }
    HRESULT STDMETHODCALLTYPE EnumExportedTypes(HCORENUM*, mdExportedType[], ULONG, ULONG*) override
    {
        return E_NOTIMPL;
    }
    HRESULT STDMETHODCALLTYPE EnumManifestResources(HCORENUM*, mdManifestResource[], ULONG, ULONG*) override
    {
        return E_NOTIMPL;
    }
    HRESULT STDMETHODCALLTYPE GetAssemblyFromScope(mdAssembly*) override
    {
        return E_NOTIMPL;
    }
    HRESULT STDMETHODCALLTYPE FindExportedTypeByName(LPCWSTR, mdToken, mdExportedType*) override
    {
        return E_NOTIMPL;
    }
    HRESULT STDMETHODCALLTYPE FindManifestResourceByName(LPCWSTR, mdManifestResource*) override
    {
        return E_NOTIMPL;
    }
    HRESULT STDMETHODCALLTYPE FindAssembliesByName(LPCWSTR, LPCWSTR, LPCWSTR, IUnknown*[], ULONG, ULONG*) override
    {
        return E_NOTIMPL;
    }
};
} // namespace

class AssemblyReferenceEnumerationTest : public ::testing::Test
{
protected:
    std::unordered_map<WSTRING, AssemblyVersionRedirection> redirects_;
    ComPtr<IMetaDataAssemblyImport>                         assembly_import_;
    AssemblyImportStub*                                     import_ = nullptr;

    void SetUp() override
    {
        import_ = new AssemblyImportStub;
        assembly_import_.Attach(import_);
    }

    void Redirect()
    {
        RedirectAssemblyReferences(assembly_import_, {}, redirects_);
        EXPECT_EQ(import_->close_calls, 1);
        EXPECT_EQ(import_->closed_handle, static_cast<HCORENUM>(import_));
    }
};

TEST_F(AssemblyReferenceEnumerationTest, StopsOnInitialFailure)
{
    import_->failure_call = 1;

    Redirect();

    EXPECT_EQ(import_->enum_calls, 1);
    EXPECT_TRUE(import_->inspected_refs.empty());
}

TEST_F(AssemblyReferenceEnumerationTest, StopsOnFailureWithoutReusingPreviousBatch)
{
    import_->assembly_refs = {TokenFromRid(1, mdtAssemblyRef), TokenFromRid(2, mdtAssemblyRef)};
    import_->failure_call  = 2;

    Redirect();

    EXPECT_EQ(import_->enum_calls, 2);
    EXPECT_EQ(import_->inspected_refs, import_->assembly_refs);
}

TEST_F(AssemblyReferenceEnumerationTest, InspectsAllSuccessfulBatchesOnce)
{
    for (ULONG i = 1; i <= kEnumeratorMax + 1; ++i)
    {
        import_->assembly_refs.push_back(TokenFromRid(i, mdtAssemblyRef));
    }

    Redirect();

    EXPECT_EQ(import_->enum_calls, 3);
    EXPECT_EQ(import_->inspected_refs, import_->assembly_refs);
}

TEST_F(AssemblyReferenceEnumerationTest, StopsOnEmptyEnumeration)
{
    Redirect();

    EXPECT_EQ(import_->enum_calls, 1);
    EXPECT_TRUE(import_->inspected_refs.empty());
}

TEST_F(AssemblyReferenceEnumerationTest, StopsOnSuccessfulEmptyBatch)
{
    import_->empty_result = S_OK;

    Redirect();

    EXPECT_EQ(import_->enum_calls, 1);
    EXPECT_TRUE(import_->inspected_refs.empty());
}
