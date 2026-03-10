// Copyright The OpenTelemetry Authors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.Identity;
#endif

namespace TestApplication.Http;

#pragma warning disable CA1812 // Avoid uninstantiated internal classes. This class is instantiated by app builder.
internal sealed class Startup
#pragma warning restore CA1812 // Avoid uninstantiated internal classes. This class is instantiated by app builder.
{
    private static readonly ActivitySource MyActivitySource = new("TestApplication.Http", "1.0.0");

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("TestPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
            });
        });

#if NET10_0_OR_GREATER
        // Add Blazor Server to enable Components metrics
        services.AddRazorPages();
        services.AddServerSideBlazor();

        // Add Identity to enable Microsoft.AspNetCore.Identity metrics
        services.AddIdentityCore<TestUser>()
            .AddUserManager<UserManager<TestUser>>();

        // Add in-memory user store for testing
        services.AddSingleton<IUserStore<TestUser>, InMemoryUserStore>();
#endif
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app
            .UseRouting() // enables metrics for Microsoft.AspNetCore.Routing in .NET8+
            .UseAuthentication() // enables metrics for Microsoft.AspNetCore.Authentication in .NET10+
            .UseExceptionHandler(new ExceptionHandlerOptions { ExceptionHandler = _ => Task.CompletedTask }) // together with call to /exception enables metrics for Microsoft.AspNetCore.Diagnostics for .NET8+
            .UseRateLimiter() // enables metrics for Microsoft.AspNetCore.RateLimiting in .NET8+
            .UseAuthorization() // enables metrics for Microsoft.AspNetCore.Authorization in .NET10+
            .UseEndpoints(endpoints =>
            {
                endpoints.MapHub<TestHub>("/signalr"); // together with connection to SignalR Hub enables metrics for Microsoft.AspNetCore.Http.Connections for .NET8
#if NET10_0_OR_GREATER
                endpoints.MapBlazorHub(); // enables metrics for Microsoft.AspNetCore.Components.Server.Circuits in .NET10+
                endpoints.MapFallbackToPage("/_Host"); // enables metrics for Microsoft.AspNetCore.Components in .NET10+
#endif

                endpoints.Map(
                    "/test",
                    async context =>
                {
                    using (var activity = MyActivitySource.StartActivity("manual span"))
                    {
                        activity?.SetTag("test_tag", "test_value");
                    }

                    context.Response.Headers.Append("Custom-Response-Test-Header1", "Test-Value1");
                    context.Response.Headers.Append("Custom-Response-Test-Header2", "Test-Value2");
                    context.Response.Headers.Append("Custom-Response-Test-Header3", "Test-Value3");

                    await context.Response.WriteAsync("Pong").ConfigureAwait(false);
                });

                endpoints.Map("/protected", async context =>
                {
                    var authorizationService = context.RequestServices.GetRequiredService<IAuthorizationService>();
                    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                    var result = await authorizationService.AuthorizeAsync(context.User, policy).ConfigureAwait(false);

                    if (result.Succeeded)
                    {
                        await context.Response.WriteAsync("Protected").ConfigureAwait(false);
                    }
                    else
                    {
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsync("Forbidden").ConfigureAwait(false);
                    }
                });

                endpoints.Map("/login", async context =>
                {
                    var authenticationService = context.RequestServices.GetRequiredService<IAuthenticationService>();
                    var claimsPrincipal = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            [new Claim(ClaimTypes.Name, "TestUser")],
                            CookieAuthenticationDefaults.AuthenticationScheme));
                    var authProperties = new AuthenticationProperties();
                    await authenticationService.SignInAsync(context, CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal, authProperties).ConfigureAwait(false);
                    await context.Response.WriteAsync("Logged in").ConfigureAwait(false);
                });

                endpoints.Map("/logout", async context =>
                {
                    var authenticationService = context.RequestServices.GetRequiredService<IAuthenticationService>();
                    var authProperties = new AuthenticationProperties();
                    await authenticationService.SignOutAsync(context, CookieAuthenticationDefaults.AuthenticationScheme, authProperties).ConfigureAwait(false);
                    await context.Response.WriteAsync("Logged out").ConfigureAwait(false);
                });

#if NET10_0_OR_GREATER
                // Endpoints to trigger Identity metrics
                endpoints.Map("/identity/create-user", async context =>
                {
                    var userManager = context.RequestServices.GetRequiredService<UserManager<TestUser>>();
                    var user = new TestUser
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserName = "testuser" + Guid.NewGuid().ToString("N")[..8],
                        Email = "test@example.com"
                    };
                    var result = await userManager.CreateAsync(user, "Password123!").ConfigureAwait(false);
                    await context.Response.WriteAsync(result.Succeeded ? "User created" : "Failed to create user").ConfigureAwait(false);
                });

                endpoints.Map("/identity/find-user", async context =>
                {
                    var userManager = context.RequestServices.GetRequiredService<UserManager<TestUser>>();
                    var user = await userManager.FindByNameAsync("testuser").ConfigureAwait(false);
                    await context.Response.WriteAsync(user != null ? "User found" : "User not found").ConfigureAwait(false);
                });
#endif

                endpoints.Map(
                    "/exception",
                    _ => throw new InvalidOperationException("Just to throw something"));
            });
    }
}
