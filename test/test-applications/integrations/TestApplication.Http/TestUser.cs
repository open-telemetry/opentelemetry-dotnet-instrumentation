// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.Identity;

namespace TestApplication.Http;

public class TestUser : IdentityUser
{
}
#endif
