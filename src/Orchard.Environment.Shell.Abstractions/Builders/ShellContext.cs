﻿using Microsoft.Extensions.DependencyInjection;
using Orchard.Environment.Shell;
using Orchard.Environment.Shell.Builders.Models;
using System;

namespace Orchard.Hosting.ShellBuilders
{
    /// <summary>
    /// The shell context represents the shell's state that is kept alive
    /// for the whole life of the application
    /// </summary>
    public class ShellContext : IDisposable
    {
        private bool _disposed = false;

        public ShellSettings Settings { get; set; }
        public ShellBlueprint Blueprint { get; set; }
        public IServiceProvider ServiceProvider { get; set; }

        public bool IsActivated { get; set; }

        /// <summary>
        /// Creates a standalone service scope that can be used to resolve local services.
        /// </summary>
        public IServiceScope CreateServiceScope()
        {
            return ServiceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Disposes all the services registered for this shell
                    (ServiceProvider as IDisposable).Dispose();
                }

                Settings = null;
                Blueprint = null;

                IsActivated = false;

                _disposed = true;
            }
        }

        ~ShellContext()
        {
            Dispose(false);
        }
    }
}