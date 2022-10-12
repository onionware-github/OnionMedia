/*
 * Copyright (C) 2022 Jaden Phil Nebel (Onionware)
 *
 * This file is part of OnionMedia.
 * OnionMedia is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License as published by the Free Software Foundation, version 3.

 * OnionMedia is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with OnionMedia. If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Threading.Tasks;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using OnionMedia.Core.Services;

namespace OnionMedia.Services
{
    sealed class DispatcherService : IDispatcherService
    {
        public void Enqueue(Action action)
        {
            queue.TryEnqueue(new(action));
        }

        public async Task EnqueueAsync(Action action)
        {
            await queue.EnqueueAsync(action);
        }

        private readonly DispatcherQueue queue = DispatcherQueue.GetForCurrentThread();
    }
}
