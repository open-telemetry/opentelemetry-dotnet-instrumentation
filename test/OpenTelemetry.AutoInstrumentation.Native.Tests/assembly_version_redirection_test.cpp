#ifdef _WIN32
#include "pch.h"

#include "../../src/OpenTelemetry.AutoInstrumentation.Native/clr_helpers.h"

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
#endif
