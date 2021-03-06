// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_err.h"
#include "pal_utilities.h"

extern "C" void CryptoNative_ErrClearError()
{
    ERR_clear_error();
}

extern "C" uint64_t CryptoNative_ErrGetError()
{
    return ERR_get_error();
}

extern "C" uint64_t CryptoNative_ErrGetErrorAlloc(int32_t* isAllocFailure)
{
    unsigned long err = ERR_get_error();

    if (isAllocFailure)
    {
        *isAllocFailure = ERR_GET_REASON(err) == ERR_R_MALLOC_FAILURE;
    }

    return err;
}

extern "C" uint64_t CryptoNative_ErrPeekError()
{
    return ERR_peek_error();
}

extern "C" uint64_t CryptoNative_ErrPeekLastError()
{
    return ERR_peek_last_error();
}

extern "C" const char* CryptoNative_ErrReasonErrorString(uint64_t error)
{
    return ERR_reason_error_string(static_cast<unsigned long>(error));
}

extern "C" void CryptoNative_ErrErrorStringN(uint64_t e, char* buf, int32_t len)
{
    ERR_error_string_n(static_cast<unsigned long>(e), buf, UnsignedCast(len));
}
