/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OnionMedia.Core;

public sealed class IoC
{
    public static IoC Default { get; } = new();
    private IServiceProvider iocProvider;
    private readonly object lockObject = new();

    public void InitializeServices(IServiceProvider serviceProvider)
    {
        if (iocProvider != null)
            throw new InvalidOperationException("Service provider has already been initialized.");

        lock (lockObject)
        {
            iocProvider ??= serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }
    }

    public void InitializeDefault(Action<IServiceCollection> configureServices)
    {
        var serviceCollection = new ServiceCollection();
        configureServices(serviceCollection);
        InitializeServices(serviceCollection.BuildServiceProvider());
    }

    public T? GetService<T>() where T : class
    {
        if (iocProvider == null)
            throw new Exception("Service provider is not initialized.");

        return iocProvider.GetService<T>();
    }

    public T GetRequiredService<T>() where T : class
    {
        if (iocProvider == null)
            throw new Exception("Service provider is not initialized.");

        return (T)iocProvider.GetService(typeof(T))
            ?? throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
    }
}

