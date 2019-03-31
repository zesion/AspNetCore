// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Routing
{
    internal class DynamicControllerEndpointSelector : IDisposable
    {
        private readonly ActionSelector _actionSelector;
        private readonly ControllerActionEndpointDataSource _dataSource;
        private readonly DataSourceDependentCache<ActionSelectionTable<Endpoint>> _cache;

        public DynamicControllerEndpointSelector(ControllerActionEndpointDataSource dataSource, ActionSelector actionSelector)
        {
            if (dataSource == null)
            {
                throw new ArgumentNullException(nameof(dataSource));
            }

            if (actionSelector == null)
            {
                throw new ArgumentNullException(nameof(actionSelector));
            }

            _dataSource = dataSource;
            _actionSelector = actionSelector;

            _cache = new DataSourceDependentCache<ActionSelectionTable<Endpoint>>(dataSource, Initialize);
        }

        private ActionSelectionTable<Endpoint> Table => _cache.EnsureInitialized();

        public IReadOnlyList<Endpoint> SelectEndpoints(RouteValueDictionary values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var table = Table;
            var matches = table.Select(values);
            return matches;
        }

        public Endpoint SelectBestEndpoint(HttpContext httpContext, RouteValueDictionary values, IReadOnlyList<Endpoint> endpoints)
        {
            if (endpoints == null)
            {
                throw new ArgumentNullException(nameof(endpoints));
            }

            var context = new RouteContext(httpContext);
            context.RouteData = new RouteData(values);

            var actions = new ActionDescriptor[endpoints.Count];
            for (var i = 0; i < endpoints.Count; i++)
            {
                actions[i] = endpoints[i].Metadata.GetMetadata<ActionDescriptor>();
            }

            // SelectBestCandidate throws for ambiguities so we don't have to handle that here.
            var action = _actionSelector.SelectBestCandidate(context, actions);
            if (action == null)
            {
                return null;
            }

            for (var i = 0; i <actions.Length; i++)
            {
                if (object.ReferenceEquals(action, actions[i]))
                {
                    return endpoints[i];
                }
            }

            // This should never happen. We need to do *something* here for the code to compile, so throwing.
            throw new InvalidOperationException("ActionSelector returned an action that was not a candidate.");
        }

        private static ActionSelectionTable<Endpoint> Initialize(IReadOnlyList<Endpoint> endpoints)
        {
            return ActionSelectionTable<Endpoint>.Create(endpoints);
        }

        public void Dispose()
        {
            _cache.Dispose();
        }
    }
}
