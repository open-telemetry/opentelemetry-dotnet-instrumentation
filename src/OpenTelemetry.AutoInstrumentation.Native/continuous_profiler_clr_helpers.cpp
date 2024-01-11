/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#include "continuous_profiler_clr_helpers.h"

#include <cstring>

#include "otel_profiler_constants.h"
#include "environment_variables.h"
#include "logger.h"
#include "macros.h"
#include <set>
#include <stack>

#include "pal.h"

// this file contains optimized, specified version of code from clr_helpers.cpp

namespace continuous_profiler
{
FunctionInfo GetFunctionInfo(const ComPtr<IMetaDataImport2>& metadata_import, const mdToken& token)
{
    mdToken parent_token = mdTokenNil;
    WCHAR   function_name[trace::kNameMaxSize]{};
    DWORD   function_name_len = 0;

    PCCOR_SIGNATURE raw_signature;
    ULONG           raw_signature_len;

    HRESULT hr = E_FAIL;
    switch (const auto token_type = TypeFromToken(token))
    {
        case mdtMemberRef:
            hr = metadata_import->GetMemberRefProps(token, &parent_token, function_name, trace::kNameMaxSize,
                                                    &function_name_len, &raw_signature, &raw_signature_len);
            break;
        case mdtMethodDef:
            hr = metadata_import->GetMemberProps(token, &parent_token, function_name, trace::kNameMaxSize,
                                                 &function_name_len, nullptr, &raw_signature, &raw_signature_len,
                                                 nullptr, nullptr, nullptr, nullptr, nullptr);
            break;
        case mdtMethodSpec:
        {
            hr = metadata_import->GetMethodSpecProps(token, &parent_token, &raw_signature, &raw_signature_len);
            if (FAILED(hr))
            {
                return {};
            }
            const auto generic_info = GetFunctionInfo(metadata_import, parent_token);
            std::memcpy(function_name, generic_info.name.c_str(), sizeof(WCHAR) * (generic_info.name.length() + 1));
            function_name_len = DWORD(generic_info.name.length() + 1);
        }
        break;
        default:
            trace::Logger::Warn("[trace::GetFunctionInfo] unknown token type: {}", token_type);
            return {};
    }

    if (FAILED(hr) || function_name_len == 0)
    {
        return {};
    }

    // parent_token could be: TypeDef, TypeRef, TypeSpec, ModuleRef, MethodDef
    const auto type_info = GetTypeInfo(metadata_import, parent_token);

    return {token, trace::WSTRING(function_name), type_info, FunctionMethodSignature(raw_signature, raw_signature_len)};
}

TypeInfo GetTypeInfo(const ComPtr<IMetaDataImport2>& metadata_import, const mdToken& token)
{
    std::shared_ptr<TypeInfo> parentTypeInfo    = nullptr;
    mdToken                   parent_type_token = mdTokenNil;
    WCHAR                     type_name[trace::kNameMaxSize]{};
    DWORD                     type_name_len = 0;
    DWORD                     type_flags;

    HRESULT hr = E_FAIL;

    switch (const auto token_type = TypeFromToken(token))
    {
        case mdtTypeDef:
            hr = metadata_import->GetTypeDefProps(token, type_name, trace::kNameMaxSize, &type_name_len, &type_flags,
                                                  nullptr);

            metadata_import->GetNestedClassProps(token, &parent_type_token);
            if (parent_type_token != mdTokenNil)
            {
                parentTypeInfo = std::make_shared<TypeInfo>(GetTypeInfo(metadata_import, parent_type_token));
            }
            break;
        case mdtTypeRef:
            hr = metadata_import->GetTypeRefProps(token, nullptr, type_name, trace::kNameMaxSize, &type_name_len);
            break;
        case mdtTypeSpec:
        {
            PCCOR_SIGNATURE signature{};
            ULONG           signature_length{};

            hr = metadata_import->GetTypeSpecFromToken(token, &signature, &signature_length);

            if (FAILED(hr) || signature_length < 3)
            {
                return {};
            }

            if (signature[0] & ELEMENT_TYPE_GENERICINST)
            {
                mdToken type_token;
                CorSigUncompressToken(&signature[2], &type_token);
                const auto baseType = GetTypeInfo(metadata_import, type_token);
                return {baseType.id, baseType.name};
            }
        }
        break;
        case mdtModuleRef:
            metadata_import->GetModuleRefProps(token, type_name, trace::kNameMaxSize, &type_name_len);
            break;
        case mdtMemberRef:
        case mdtMethodDef:
            return GetFunctionInfo(metadata_import, token).type;
    }

    if (FAILED(hr) || type_name_len == 0)
    {
        return {};
    }

    trace::WSTRING type_name_string;

    if (parentTypeInfo != nullptr)
    {
        type_name_string = parentTypeInfo->name + name_separator + trace::WSTRING(type_name);
    }
    else
    {
        type_name_string = trace::WSTRING(type_name);
    }
    return {token, type_name_string};
}

trace::WSTRING ExtractParameterName(PCCOR_SIGNATURE&                pb_cur,
                                    const ComPtr<IMetaDataImport2>& metadata_import,
                                    const mdGenericParam*           generic_parameters)
{
    pb_cur++;
    ULONG num = 0;
    pb_cur += CorSigUncompressData(pb_cur, &num);
    if (num >= kGenericParamsMaxLen)
    {
        return kUnknown;
    }
    WCHAR      param_type_name[kParamNameMaxLen]{};
    ULONG      pch_name = 0;
    const auto hr = metadata_import->GetGenericParamProps(generic_parameters[num], nullptr, nullptr, nullptr, nullptr,
                                                          param_type_name, kParamNameMaxLen, &pch_name);
    if (FAILED(hr))
    {
        trace::Logger::Debug("GetGenericParamProps failed. HRESULT=0x", std::setfill('0'), std::setw(8), std::hex, hr);
        return kUnknown;
    }
    return param_type_name;
}

trace::WSTRING GetSigTypeTokNameNew(PCCOR_SIGNATURE&                pb_cur,
                                    const ComPtr<IMetaDataImport2>& metadata_import,
                                    mdGenericParam                  class_params[],
                                    mdGenericParam                  method_params[])
{
    trace::WSTRING token_name = trace::EmptyWStr;
    bool           ref_flag   = false;
    if (*pb_cur == ELEMENT_TYPE_BYREF)
    {
        pb_cur++;
        ref_flag = true;
    }

    bool pointer_flag = false;
    if (*pb_cur == ELEMENT_TYPE_PTR)
    {
        pb_cur++;
        pointer_flag = true;
    }

    switch (*pb_cur)
    {
        case ELEMENT_TYPE_BOOLEAN:
            token_name = trace::SystemBoolean;
            pb_cur++;
            break;
        case ELEMENT_TYPE_CHAR:
            token_name = trace::SystemChar;
            pb_cur++;
            break;
        case ELEMENT_TYPE_I1:
            token_name = trace::SystemSByte;
            pb_cur++;
            break;
        case ELEMENT_TYPE_U1:
            token_name = trace::SystemByte;
            pb_cur++;
            break;
        case ELEMENT_TYPE_U2:
            token_name = trace::SystemUInt16;
            pb_cur++;
            break;
        case ELEMENT_TYPE_I2:
            token_name = trace::SystemInt16;
            pb_cur++;
            break;
        case ELEMENT_TYPE_I4:
            token_name = trace::SystemInt32;
            pb_cur++;
            break;
        case ELEMENT_TYPE_U4:
            token_name = trace::SystemUInt32;
            pb_cur++;
            break;
        case ELEMENT_TYPE_I8:
            token_name = trace::SystemInt64;
            pb_cur++;
            break;
        case ELEMENT_TYPE_U8:
            token_name = trace::SystemUInt64;
            pb_cur++;
            break;
        case ELEMENT_TYPE_R4:
            token_name = trace::SystemSingle;
            pb_cur++;
            break;
        case ELEMENT_TYPE_R8:
            token_name = trace::SystemDouble;
            pb_cur++;
            break;
        case ELEMENT_TYPE_I:
            token_name = trace::SystemIntPtr;
            pb_cur++;
            break;
        case ELEMENT_TYPE_U:
            token_name = trace::SystemUIntPtr;
            pb_cur++;
            break;
        case ELEMENT_TYPE_STRING:
            token_name = trace::SystemString;
            pb_cur++;
            break;
        case ELEMENT_TYPE_OBJECT:
            token_name = trace::SystemObject;
            pb_cur++;
            break;
        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VALUETYPE:
        {
            pb_cur++;
            mdToken token;
            pb_cur += CorSigUncompressToken(pb_cur, &token);
            token_name = GetTypeInfo(metadata_import, token).name;
            break;
        }
        case ELEMENT_TYPE_SZARRAY:
        {
            pb_cur++;
            token_name = GetSigTypeTokNameNew(pb_cur, metadata_import, class_params, method_params) + WStr("[]");
            break;
        }
        case ELEMENT_TYPE_GENERICINST:
        {
            pb_cur++;
            token_name = GetSigTypeTokNameNew(pb_cur, metadata_import, class_params, method_params);
            token_name += kGenericParamsOpeningBrace;
            ULONG num = 0;
            pb_cur += CorSigUncompressData(pb_cur, &num);
            for (ULONG i = 0; i < num; i++)
            {
                token_name += GetSigTypeTokNameNew(pb_cur, metadata_import, class_params, method_params);
                if (i != num - 1)
                {
                    token_name += kParamsSeparator;
                }
            }
            token_name += kGenericParamsClosingBrace;
            break;
        }
        case ELEMENT_TYPE_MVAR:
        {
            token_name += ExtractParameterName(pb_cur, metadata_import, method_params);
            break;
        }
        case ELEMENT_TYPE_VAR:
        {
            token_name += ExtractParameterName(pb_cur, metadata_import, class_params);
            break;
        }
        default:
            break;
    }

    if (ref_flag)
    {
        token_name += WStr("&");
    }

    if (pointer_flag)
    {
        token_name += WStr("*");
    }
    return token_name;
}

trace::WSTRING TypeSignature::GetTypeTokName(ComPtr<IMetaDataImport2>& pImport,
                                             mdGenericParam            class_params[],
                                             mdGenericParam            method_params[]) const
{
    PCCOR_SIGNATURE pbCur = &pbBase[offset];
    return GetSigTypeTokNameNew(pbCur, pImport, class_params, method_params);
}

HRESULT FunctionMethodSignature::TryParse()
{
    PCCOR_SIGNATURE pbCur = pbBase;
    PCCOR_SIGNATURE pbEnd = pbBase + len;
    unsigned char   elem_type;

    IfFalseRetFAIL(trace::ParseByte(pbCur, pbEnd, &elem_type));

    if (elem_type & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        unsigned gen_param_count;
        IfFalseRetFAIL(trace::ParseNumber(pbCur, pbEnd, &gen_param_count));
    }

    unsigned param_count;
    IfFalseRetFAIL(trace::ParseNumber(pbCur, pbEnd, &param_count));

    IfFalseRetFAIL(trace::ParseRetType(pbCur, pbEnd));

    auto fEncounteredSentinal = false;
    for (unsigned i = 0; i < param_count; i++)
    {
        if (pbCur >= pbEnd)
            return E_FAIL;

        if (*pbCur == ELEMENT_TYPE_SENTINEL)
        {
            if (fEncounteredSentinal)
                return E_FAIL;

            fEncounteredSentinal = true;
            pbCur++;
        }

        const PCCOR_SIGNATURE pbParam = pbCur;

        IfFalseRetFAIL(trace::ParseParamOrLocal(pbCur, pbEnd));

        TypeSignature argument{};
        argument.pbBase = pbBase;
        argument.length = (ULONG)(pbCur - pbParam);
        argument.offset = (ULONG)(pbCur - pbBase - argument.length);

        params.push_back(argument);
    }

    return S_OK;
}
} // namespace continuous_profiler
