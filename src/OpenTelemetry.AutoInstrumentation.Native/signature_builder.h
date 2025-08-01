/*
 * Copyright The OpenTelemetry Authors
 * SPDX-License-Identifier: Apache-2.0
 */

#ifndef OTEL_CLR_PROFILER_SIGNATURE_BUILDER_H_
#define OTEL_CLR_PROFILER_SIGNATURE_BUILDER_H_

#include <vector>
#include <corhlpr.h>

namespace trace
{

class SignatureData
{
protected:
    std::vector<COR_SIGNATURE> blob_;

public:
    SignatureData() = default;
    SignatureData(std::initializer_list<COR_SIGNATURE> bytes) : blob_(bytes) {}
    SignatureData(std::initializer_list<SignatureData> elements)
    {
        for (auto& inner : elements)
        {
            blob_.insert(blob_.end(), inner.blob_.begin(), inner.blob_.end());
        }
    }

    PCCOR_SIGNATURE Head() const
    {
        return blob_.data();
    }

    ULONG Size() const
    {
        return static_cast<ULONG>(blob_.size());
    }
};

class SignatureBuilder : public virtual SignatureData
{
public:
    SignatureBuilder() = default;
    SignatureBuilder(std::initializer_list<COR_SIGNATURE> bytes) : SignatureData(bytes) {}
    SignatureBuilder(std::initializer_list<SignatureData> elements) : SignatureData(elements) {}

    SignatureBuilder& PushRawByte(COR_SIGNATURE byte)
    {
        blob_.push_back(byte);
        return *this;
    }

    template <class InputIt>
    SignatureBuilder& PushRawBytes(InputIt first, InputIt last)
    {
        blob_.insert(blob_.end(), first, last);
        return *this;
    }

    SignatureBuilder& PushRawBytes(std::initializer_list<COR_SIGNATURE> bytes)
    {
        blob_.insert(blob_.end(), bytes);
        return *this;
    }

    SignatureBuilder& PushCompressedData(ULONG data)
    {
        COR_SIGNATURE compressed[sizeof(ULONG)];
        ULONG         compressedSize = CorSigCompressData(data, compressed);
        for (ULONG i = 0; i < compressedSize; i++)
        {
            PushRawByte(compressed[i]);
        }
        return *this;
    }

    SignatureBuilder& PushToken(mdToken token)
    {
        COR_SIGNATURE compressed[sizeof(mdToken)];
        ULONG         compressedSize = CorSigCompressToken(token, compressed);
        for (ULONG i = 0; i < compressedSize; i++)
        {
            PushRawByte(compressed[i]);
        }
        return *this;
    }

    SignatureBuilder& Push(const SignatureData& inner)
    {
        blob_.insert(blob_.end(), inner.Head(), inner.Head()+inner.Size());
        return *this;
    }

    enum class MethodCallingConvection : COR_SIGNATURE
    {
        Static   = IMAGE_CEE_CS_CALLCONV_DEFAULT,
        Instance = IMAGE_CEE_CS_CALLCONV_HASTHIS
    };

    enum class TokenTypeMode : COR_SIGNATURE
    {
        ValueType = ELEMENT_TYPE_VALUETYPE,
        Class     = ELEMENT_TYPE_CLASS
    };

    enum class GenericArgMode : COR_SIGNATURE
    {
        Type   = ELEMENT_TYPE_VAR,
        Method = ELEMENT_TYPE_MVAR
    };

    enum class BuiltIn : COR_SIGNATURE
    {
        Void    = ELEMENT_TYPE_VOID,
        Boolean = ELEMENT_TYPE_BOOLEAN,
        Char    = ELEMENT_TYPE_CHAR,
        SByte   = ELEMENT_TYPE_I1,
        Byte    = ELEMENT_TYPE_U1,
        Int16   = ELEMENT_TYPE_I2,
        UInt16  = ELEMENT_TYPE_U2,
        Int32   = ELEMENT_TYPE_I4,
        UInt32  = ELEMENT_TYPE_U4,
        Int64   = ELEMENT_TYPE_I8,
        UInt64  = ELEMENT_TYPE_U8,
        Float   = ELEMENT_TYPE_R4,
        Double  = ELEMENT_TYPE_R8,
        String  = ELEMENT_TYPE_STRING,
        IntPtr  = ELEMENT_TYPE_I,
        UintPtr = ELEMENT_TYPE_U,
        Object  = ELEMENT_TYPE_OBJECT
    };

    class Type;
    class TokenType;
    class ValueType;
    class Class;
    class Array;
    class ByRef;
    class GenericArgument;
    class GenericArgumentOfMethod;
    class GenericArgumentOfType;
    class GenericInstance;
    class Field;
    class Method;
    class InstanceMethod;
    class StaticMethod;
};

class SignatureBuilder::Type : public virtual SignatureData, protected SignatureBuilder
{
public:
    Type(BuiltIn builtin)
        : SignatureData{static_cast<COR_SIGNATURE>(builtin)}
    {
    }

protected:
    Type() = default;
};

class SignatureBuilder::TokenType : public Type
{
public:
    TokenType(TokenTypeMode type, mdToken token) 
    {
        this->PushRawByte(static_cast<COR_SIGNATURE>(type));
        this->PushToken(token);
    }
};

class SignatureBuilder::ValueType : public TokenType
{
public:
    explicit ValueType(mdToken token) : TokenType(TokenTypeMode::ValueType, token) {}
};

class SignatureBuilder::Class : public TokenType
{
public:
    explicit Class(mdToken token) : TokenType(TokenTypeMode::Class, token) {}
};

class SignatureBuilder::Array : public Type
{
public:
    explicit Array(const Type& type)
    {
        this->PushRawByte(ELEMENT_TYPE_SZARRAY);
        this->Push(type);
    }
};

class SignatureBuilder::ByRef : public Type
{
public:
    explicit ByRef(const Type& type)
    {
        this->PushRawByte(ELEMENT_TYPE_BYREF);
        this->Push(type);
    }
};

class SignatureBuilder::GenericArgument : public Type
{
public:
    GenericArgument(GenericArgMode type, UINT num)
    {
        this->PushRawByte(static_cast<COR_SIGNATURE>(type));
        this->PushCompressedData(num);
    }
};

class SignatureBuilder::GenericArgumentOfMethod : public GenericArgument
{
public:
    explicit GenericArgumentOfMethod(UINT num) : GenericArgument(GenericArgMode::Method, num) {}
};

class SignatureBuilder::GenericArgumentOfType : public GenericArgument
{
public:
    explicit GenericArgumentOfType(UINT num) : GenericArgument(GenericArgMode::Type, num) {}
};

class SignatureBuilder::GenericInstance : public Type
{
public:
    GenericInstance(const TokenType& open_generic, const std::vector<Type>& generic_args)
    {
        this->PushRawByte(ELEMENT_TYPE_GENERICINST);
        this->Push(open_generic);
        this->PushCompressedData(static_cast<ULONG>(generic_args.size()));
        for (const auto& arg : generic_args)
        {
            this->Push(arg);
        }
    }
};

class SignatureBuilder::Field : public virtual SignatureData, protected SignatureBuilder
{
public:
    explicit Field(const Type& field_type)
    {
        this->PushRawByte(IMAGE_CEE_CS_CALLCONV_FIELD);
        this->Push(field_type);
    }
};

class SignatureBuilder::Method : public virtual SignatureData, protected SignatureBuilder
{
public:
    Method(MethodCallingConvection calling_convection, const Type& return_type, const std::vector<Type>& args)
    {
        this->PushRawByte(static_cast<COR_SIGNATURE>(calling_convection));
        this->PushCompressedData(static_cast<ULONG>(args.size()));
        this->Push(return_type);
        for (const auto& arg : args)
        {
            this->Push(arg);
        }
    }
};

class SignatureBuilder::InstanceMethod : public Method
{
public:
    InstanceMethod(const Type& return_type, const std::vector<Type>& args)
        : Method(MethodCallingConvection::Instance, return_type, args)
    {
    }
};

class SignatureBuilder::StaticMethod : public Method
{
public:
    StaticMethod(const Type& return_type, const std::vector<Type>& args)
        : Method(MethodCallingConvection::Static, return_type, args)
    {
    }
};


} // namespace trace

#endif // OTEL_CLR_PROFILER_SIGNATURE_BUILDER_H_
