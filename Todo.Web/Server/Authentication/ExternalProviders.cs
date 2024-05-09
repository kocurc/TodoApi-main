﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Todo.Web.Server.Authentication
{
    public class ExternalProviders(IAuthenticationSchemeProvider schemeProvider)
    {
        private Task<string[]>? _providerNames;

        public Task<string[]> GetProviderNamesAsync()
        {
            return _providerNames ??= GetProviderNamesAsyncCore();
        }

        private async Task<string[]> GetProviderNamesAsyncCore()
        {
            List<string>? providerNames = null;

            var schemes = await schemeProvider.GetAllSchemesAsync();

            foreach (var s in schemes)
            {
                // We're assuming all schemes that aren't cookies are social
                if (s.Name == CookieAuthenticationDefaults.AuthenticationScheme ||
                    s.Name == AuthenticationSchemes.ExternalScheme)
                {
                    continue;
                }

                providerNames ??= new();
                providerNames.Add(s.Name);
            }

            return providerNames?.ToArray() ?? Array.Empty<string>();
        }
    }
}
