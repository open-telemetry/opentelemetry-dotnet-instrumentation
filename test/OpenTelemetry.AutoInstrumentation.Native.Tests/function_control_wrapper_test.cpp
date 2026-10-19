// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#include "pch.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/function_control_wrapper.h"
#include "../../src/OpenTelemetry.AutoInstrumentation.Native/il_rewriter_wrapper.h"

using namespace trace;

TEST(FunctionControlWrapperTest, SuccessiveRewritersReadAndPreserveEarlierChanges)
{
    constexpr ModuleID    module = 1;
    constexpr mdMethodDef method = 0x06000001;
    // Tiny header followed by ldc.i4.1; ret.
    const BYTE                     original[] = {0x0A, CEE_LDC_I4_1, CEE_RET};
    ComPtr<FunctionControlWrapper> source;
    source.Attach(new FunctionControlWrapper(nullptr, module, method));
    ASSERT_EQ(source->SetILFunctionBody(sizeof(original), original), S_OK);

    ComPtr<FunctionControlWrapper> changes;
    changes.Attach(new FunctionControlWrapper(source.Get(), module, method));
    {
        ILRewriter first(changes.Get(), changes.Get(), module, method);
        ASSERT_EQ(first.Import(), S_OK);
        ILRewriterWrapper wrapper(&first);
        wrapper.SetILPosition(first.GetILList()->m_pNext);
        wrapper.CreateInstr(CEE_NOP);
        ASSERT_EQ(first.Export(), S_OK);
    }
    {
        ILRewriter second(changes.Get(), changes.Get(), module, method);
        ASSERT_EQ(second.Import(), S_OK);
        ASSERT_EQ(second.GetILList()->m_pNext->m_opcode, CEE_NOP);
        second.GetILList()->m_pNext->m_pNext->m_opcode = CEE_LDC_I4_2;
        ASSERT_EQ(second.Export(), S_OK);
    }

    ASSERT_EQ(changes->ApplyChanges(source.Get()), S_OK);
    ILRewriter result(source.Get(), source.Get(), module, method);
    ASSERT_EQ(result.Import(), S_OK);
    auto instruction = result.GetILList()->m_pNext;
    ASSERT_EQ(instruction->m_opcode, CEE_NOP);
    instruction = instruction->m_pNext;
    ASSERT_EQ(instruction->m_opcode, CEE_LDC_I4_2);
    ASSERT_EQ(instruction->m_pNext->m_opcode, CEE_RET);
}

TEST(FunctionControlWrapperTest, InvalidBodyDoesNotReplaceEarlierBody)
{
    ComPtr<FunctionControlWrapper> wrapper;
    wrapper.Attach(new FunctionControlWrapper(nullptr, 1, 0x06000001));
    const BYTE original[] = {0x0A, CEE_LDC_I4_1, CEE_RET};
    ASSERT_EQ(wrapper->SetILFunctionBody(sizeof(original), original), S_OK);
    EXPECT_EQ(wrapper->SetILFunctionBody(sizeof(original), nullptr), E_INVALIDARG);
    LPCBYTE body   = nullptr;
    ULONG   length = 0;
    ASSERT_EQ(wrapper->GetMethodBody(&body, &length), S_OK);
    ASSERT_EQ(length, sizeof(original));
    EXPECT_EQ(memcmp(body, original, length), 0);
}
