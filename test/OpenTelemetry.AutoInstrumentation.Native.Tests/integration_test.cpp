#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/integration.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/regex_utils.h"

#ifdef _WIN32
#include <Windows.h>
#endif

using namespace trace;

TEST(IntegrationTest, AssemblyReference)
{
    AssemblyReference ref(L"Some.Assembly, Version=1.2.3.4, Culture=notneutral, "
                          L"PublicKeyToken=0123456789abcdef");

    EXPECT_EQ(ref.name, L"Some.Assembly");
    EXPECT_EQ(ref.version, Version(1, 2, 3, 4));
    EXPECT_EQ(ref.locale, L"notneutral");
    EXPECT_EQ(ref.public_key, PublicKey({0x01, 0x23, 0x45, 0x67, 0x89, 0xab, 0xcd, 0xef}));
}

TEST(IntegrationTest, AssemblyReferenceNameOnly)
{
    AssemblyReference ref(L"Some.Assembly");

    EXPECT_EQ(ref.name, L"Some.Assembly");
    EXPECT_EQ(ref.version, Version(0, 0, 0, 0));
    EXPECT_EQ(ref.locale, L"neutral");
    EXPECT_EQ(ref.public_key, PublicKey({0, 0, 0, 0, 0, 0, 0, 0}));
}

TEST(IntegrationTest, AssemblyReferenceInvalidPublicKey)
{
    AssemblyReference ref(L"Some.Assembly, PublicKeyToken=xyz");
    EXPECT_EQ(ref.public_key, PublicKey({0, 0, 0, 0, 0, 0, 0, 0}));
}

TEST(IntegrationTest, AssemblyReferenceNullPublicKey)
{
    AssemblyReference ref(L"Some.Assembly, PublicKeyToken=null");
    EXPECT_EQ(ref.public_key, PublicKey({0, 0, 0, 0, 0, 0, 0, 0}));
}

TEST(IntegrationTest, AssemblyReferencePartialVersion)
{
    AssemblyReference ref(L"Some.Assembly, Version=1.2.3");
    EXPECT_EQ(ref.version, Version(0, 0, 0, 0));
}

TEST(IntegrationTest, AssemblyReferenceInvalidVersion)
{
    AssemblyReference ref(L"Some.Assembly, Version=xyz");
    EXPECT_EQ(ref.version, Version(0, 0, 0, 0));
}

TEST(IntegrationTest, AssemblyReferenceVersionRejectsOversizedComponent)
{
    AssemblyReference ref(WStr("Some.Assembly, Version=999999999999999999999999.2.3.4"));
    EXPECT_EQ(ref.version, Version(0, 0, 0, 0));
}

TEST(IntegrationTest, AssemblyReferenceVersionRejectsPartiallyParsedOversizedComponent)
{
    AssemblyReference ref(WStr("Some.Assembly, Version=1.65536.3.4"));
    EXPECT_EQ(ref.version, Version(0, 0, 0, 0));
}

#ifdef _WIN32
// Memory-safety regression test for ExtractPublicKeyToken: the copy loop must bound itself to
// the matched 8-byte token, not to the caller-supplied length. The 8-byte destination is placed
// at the end of a committed page backed by a PAGE_NOACCESS guard page, so an over-write faults.
namespace
{

class GuardedWriteBuffer
{
public:
    explicit GuardedWriteBuffer(size_t size)
    {
        SYSTEM_INFO si{};
        GetSystemInfo(&si);
        const size_t page = si.dwPageSize;

        // The destination must fit within the committed page so that its final byte abuts the
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
        // and the test asserts on it rather than dereferencing an invalid pointer.
        base_ = base;
        data_ = base_ + page - size;
    }

    ~GuardedWriteBuffer()
    {
        if (base_ != nullptr)
        {
            VirtualFree(base_, 0, MEM_RELEASE);
        }
    }

    GuardedWriteBuffer(const GuardedWriteBuffer&)            = delete;
    GuardedWriteBuffer& operator=(const GuardedWriteBuffer&) = delete;

    unsigned char* data() const
    {
        return data_;
    }

private:
    unsigned char* base_ = nullptr;
    unsigned char* data_ = nullptr;
};

// SEH wrapper - must not own any C++ objects requiring unwinding.
bool ExtractPublicKeyTokenFaults(const WSTRING& str, unsigned char* data, int length)
{
    __try
    {
        ExtractPublicKeyToken(str, data, length);
        return false;
    }
    __except (GetExceptionCode() == EXCEPTION_ACCESS_VIOLATION ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH)
    {
        return true;
    }
}

} // namespace

TEST(IntegrationTest, ExtractPublicKeyTokenDoesNotWritePastBufferWhenLengthExceedsMatch)
{
    // 8-byte destination, but the caller asks for 10 bytes; the match only supplies 8.
    const WSTRING      reference = L"PublicKeyToken=0123456789abcdef";
    GuardedWriteBuffer buffer(8);
    ASSERT_NE(buffer.data(), nullptr) << "Failed to set up the guard-page buffer for the test.";

    const bool faulted = ExtractPublicKeyTokenFaults(reference, buffer.data(), 10);

    ASSERT_FALSE(faulted) << "ExtractPublicKeyToken wrote past the end of the destination buffer because the copy "
                             "loop trusted the caller-supplied length instead of the matched token size.";
}
#endif
