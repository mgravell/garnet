﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Net.Security;

namespace Garnet.server.TLS
{
    /// <summary>
    /// Interface to provide Garnet TLS options
    /// </summary>
    public interface IGarnetTlsOptions
    {
        /// <summary>
        /// TLS server options
        /// </summary>
        SslServerAuthenticationOptions TlsServerOptions { get; }

        /// <summary>
        /// TLS client options, used by cluster clients
        /// </summary>
        SslClientAuthenticationOptions TlsClientOptions { get; }

        /// <summary>
        /// Update certificate file
        /// </summary>
        /// <param name="certFileName"></param>
        /// <param name="certPassword"></param>
        /// <param name="errorMessage">The ASCII error message if the method returned <see langword="false"/>; otherwise <see langword="null"/></param>
        /// <returns></returns>
        bool UpdateCertFile(string certFileName, string certPassword, out string errorMessage);
    }
}