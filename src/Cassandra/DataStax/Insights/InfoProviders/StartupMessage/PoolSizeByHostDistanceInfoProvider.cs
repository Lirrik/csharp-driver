//
//      Copyright (C) DataStax Inc.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// 

using Cassandra.Connections.Control;
using Cassandra.DataStax.Insights.Schema.StartupMessage;
using Cassandra.SessionManagement;

namespace Cassandra.DataStax.Insights.InfoProviders.StartupMessage
{
    internal class PoolSizeByHostDistanceInfoProvider : IInsightsInfoProvider<PoolSizeByHostDistance>
    {
        public PoolSizeByHostDistance GetInformation(
            IInternalCluster cluster, IInternalSession session, IInternalMetadata internalMetadata)
        {
            return new PoolSizeByHostDistance
            {
                Local = cluster
                        .Configuration
                        .GetOrCreatePoolingOptions(internalMetadata.ProtocolVersion)
                        .GetCoreConnectionsPerHost(HostDistance.Local),
                Remote = cluster
                         .Configuration
                         .GetOrCreatePoolingOptions(internalMetadata.ProtocolVersion)
                         .GetCoreConnectionsPerHost(HostDistance.Remote)
            };
        }
    }
}