// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.Cookies;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Identity
{
    public class SecurityStampValidator<TUser> : ISecurityStampValidator where TUser : class
    {
        /// <summary>
        ///     Rejects the identity if the stamp changes, and otherwise will sign in a new
        ///     ClaimsIdentity
        /// </summary>
        /// <returns></returns>
        public virtual async Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            var manager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<TUser>>();
            var userId = context.Principal.GetUserId();
            var user = await manager.ValidateSecurityStampAsync(context.Principal, userId);
            if (user != null)
            {
                await manager.SignInAsync(user, context.Properties, authenticationMethod: null);
            }
            else
            {
                context.RejectPrincipal();
                manager.SignOut();
            }
        }
    }

    /// <summary>
    ///     Static helper class used to configure a CookieAuthenticationNotifications to validate a cookie against a user's security
    ///     stamp
    /// </summary>
    public static class SecurityStampValidator
    {
        public static Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
        {
            var currentUtc = DateTimeOffset.UtcNow;
            if (context.Options != null && context.Options.SystemClock != null)
            {
                currentUtc = context.Options.SystemClock.UtcNow;
            }
            var issuedUtc = context.Properties.IssuedUtc;

            if (context.HttpContext.RequestServices == null)
            {
                throw new InvalidOperationException("TODO: RequestServices is null, missing Use[Request]Services?");
            }

            // Only validate if enough time has elapsed
            var validate = (issuedUtc == null);
            if (issuedUtc != null)
            {
                var timeElapsed = currentUtc.Subtract(issuedUtc.Value);
                var accessor = context.HttpContext.RequestServices.GetRequiredService<IOptions<IdentityOptions>>();
                validate = timeElapsed > accessor.Options.SecurityStampValidationInterval;
            }
            if (validate)
            {
                var validator = context.HttpContext.RequestServices.GetRequiredService<ISecurityStampValidator>();
                return validator.ValidateAsync(context);
            }
            return Task.FromResult(0);
        }
    }
}