// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extensions for <see cref="IEndpointRouteBuilder"/>.
    /// </summary>
    public static class ComponentEndpointRouteBuilderExtensions
    {
        /// <summary>
        ///Maps the Blazor <see cref="Hub" /> to the default path and associates
        /// the component <typeparamref name="TComponent"/> to this hub instance as the given DOM <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TComponent">The first <see cref="IComponent"/> associated with this Blazor <see cref="Hub" />.</typeparam>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="selector">The selector for the <typeparamref name="TComponent"/>.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(
            this IEndpointRouteBuilder endpoints,
            string selector) where TComponent : IComponent
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return endpoints.MapBlazorHub(typeof(TComponent), selector, ComponentHub.DefaultPath);
        }

        /// <summary>
        ///Maps the Blazor <see cref="Hub" /> to the default path and associates
        /// the component <typeparamref name="TComponent"/> to this hub instance as the given DOM <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TComponent">The first <see cref="IComponent"/> associated with this Blazor <see cref="Hub" />.</typeparam>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="selector">The selector for the <typeparamref name="TComponent"/>.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(
            this IEndpointRouteBuilder endpoints,
            string selector,
            Action<HttpConnectionDispatcherOptions> configureOptions) where TComponent : IComponent
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            return endpoints.MapBlazorHub(typeof(TComponent), selector, ComponentHub.DefaultPath, configureOptions);
        }

        /// <summary>
        ///Maps the Blazor <see cref="Hub" /> to the default path and associates
        /// the component <paramref name="type"/> to this hub instance as the given DOM <paramref name="selector"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="type">The first <see cref="IComponent"/> associated with this Blazor <see cref="Hub" />.</param>
        /// <param name="selector">The selector for the component.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub(
            this IEndpointRouteBuilder endpoints,
            Type type,
            string selector)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return endpoints.MapBlazorHub(type, selector, ComponentHub.DefaultPath);
        }

        /// <summary>
        ///Maps the Blazor <see cref="Hub" /> to the default path and associates
        /// the component <paramref name="type"/> to this hub instance as the given DOM <paramref name="selector"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="type">The first <see cref="IComponent"/> associated with this Blazor <see cref="Hub" />.</param>
        /// <param name="selector">The selector for the component.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub(
            this IEndpointRouteBuilder endpoints,
            Type type,
            string selector,
            Action<HttpConnectionDispatcherOptions> configureOptions)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            return endpoints.MapBlazorHub(type, selector, ComponentHub.DefaultPath, configureOptions);
        }

        /// <summary>
        /// Maps the Blazor <see cref="Hub" /> to the path <paramref name="path"/> and associates
        /// the component <typeparamref name="TComponent"/> to this hub instance as the given DOM <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TComponent">The first <see cref="IComponent"/> associated with this Blazor <see cref="Hub" />.</typeparam>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="selector">The selector for the <typeparamref name="TComponent"/>.</param>
        /// <param name="path">The path to map the Blazor <see cref="Hub" />.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(
            this IEndpointRouteBuilder endpoints,
            string selector,
            string path) where TComponent : IComponent
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return endpoints.MapBlazorHub(typeof(TComponent), selector, path);
        }

        /// <summary>
        /// Maps the Blazor <see cref="Hub" /> to the path <paramref name="path"/> and associates
        /// the component <typeparamref name="TComponent"/> to this hub instance as the given DOM <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="TComponent">The first <see cref="IComponent"/> associated with this Blazor <see cref="Hub" />.</typeparam>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="selector">The selector for the <typeparamref name="TComponent"/>.</param>
        /// <param name="path">The path to map the Blazor <see cref="Hub" />.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub<TComponent>(
            this IEndpointRouteBuilder endpoints,
            string selector,
            string path,
            Action<HttpConnectionDispatcherOptions> configureOptions) where TComponent : IComponent
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            return endpoints.MapBlazorHub(typeof(TComponent), selector, path, configureOptions);
        }

        /// <summary>
        /// Maps the Blazor <see cref="Hub" /> to the path <paramref name="path"/> and associates
        /// the component <paramref name="componentType"/> to this hub instance as the given DOM <paramref name="selector"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="componentType">The first <see cref="IComponent"/> associated with this Blazor <see cref="Hub" />.</param>
        /// <param name="selector">The selector for the <paramref name="componentType"/>.</param>
        /// <param name="path">The path to map the Blazor <see cref="Hub" />.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub(
            this IEndpointRouteBuilder endpoints,
            Type componentType,
            string selector,
            string path)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return endpoints.MapBlazorHub(componentType, selector, path, configureOptions: _ => { });
        }

        /// <summary>
        /// Maps the Blazor <see cref="Hub" /> to the path <paramref name="path"/> and associates
        /// the component <paramref name="componentType"/> to this hub instance as the given DOM <paramref name="selector"/>.
        /// </summary>
        /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
        /// <param name="componentType">The first <see cref="IComponent"/> associated with this Blazor <see cref="Hub" />.</param>
        /// <param name="selector">The selector for the <paramref name="componentType"/>.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        /// <param name="path">The path to map the Blazor <see cref="Hub" />.</param>
        /// <returns>The <see cref="ComponentEndpointConventionBuilder"/>.</returns>
        public static ComponentEndpointConventionBuilder MapBlazorHub(
            this IEndpointRouteBuilder endpoints,
            Type componentType,
            string selector,
            string path,
            Action<HttpConnectionDispatcherOptions> configureOptions)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (componentType == null)
            {
                throw new ArgumentNullException(nameof(componentType));
            }

            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions));
            }

            var hubEndpoint = endpoints.MapHub<ComponentHub>(path, configureOptions);

            var disconnectEndpoint = endpoints.Map(
                (path.EndsWith("/") ? path : path + "/") + "disconnect/",
                endpoints.CreateApplicationBuilder().UseMiddleware<CircuitDisconnectMiddleware>().Build())
                .WithDisplayName("Blazor disconnect");

            return new ComponentEndpointConventionBuilder(
                hubEndpoint,
                disconnectEndpoint)
                    .AddComponent(componentType, selector);
        }
    }
}
