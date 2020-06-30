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

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Cassandra.Connections.Control;
using Cassandra.Serialization;
using Cassandra.SessionManagement;
using Cassandra.Tasks;

namespace Cassandra
{
    /// <inheritdoc />
    internal class Metadata : IMetadata
    {
        private const int Disposed = 10;
        private const int Initialized = 5;
        private const int Initializing = 1;
        private const int NotInitialized = 0;

        private readonly int _queryAbortTimeout;

        private volatile Task _initTask;

        private long _state = Metadata.NotInitialized;

        internal InternalMetadata InternalMetadata { get; }

        internal Metadata(
            IInternalCluster cluster,
            Configuration configuration,
            ISerializerManager serializerManager,
            IEnumerable<IContactPoint> parsedContactPoints)
        {
            _queryAbortTimeout = configuration.DefaultRequestOptions.QueryAbortTimeout;
            Configuration = configuration;
            InternalMetadata = new InternalMetadata(cluster, this, configuration, serializerManager, parsedContactPoints);
        }

        internal Metadata(
            IInternalCluster cluster,
            Configuration configuration,
            ISerializerManager serializerManager,
            IEnumerable<IContactPoint> parsedContactPoints,
            SchemaParser schemaParser)
        {
            _queryAbortTimeout = configuration.DefaultRequestOptions.QueryAbortTimeout;
            Configuration = configuration;
            InternalMetadata = new InternalMetadata(
                cluster, this, configuration, serializerManager, parsedContactPoints, schemaParser);
        }

        public Configuration Configuration { get; }

        /// <inheritdoc />
        public event HostsEventHandler HostsEvent;

        /// <inheritdoc />
        public event SchemaChangedEventHandler SchemaChangedEvent;

        /// <inheritdoc />
        public event Action<Host> HostAdded;

        /// <inheritdoc />
        public event Action<Host> HostRemoved;

        /// <inheritdoc />
        public async Task<ClusterDescription> GetClusterDescriptionAsync()
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return GetClusterDescriptionInternal();
        }

        /// <inheritdoc />
        public ClusterDescription GetClusterDescription()
        {
            TryInitialize();
            return GetClusterDescriptionInternal();
        }

        /// <inheritdoc />
        public ICollection<Host> AllHostsSnapshot()
        {
            return InternalMetadata.AllHosts();
        }

        /// <inheritdoc />
        public IEnumerable<IPEndPoint> AllReplicasSnapshot()
        {
            return InternalMetadata.AllReplicas();
        }

        /// <inheritdoc />
        public ICollection<Host> GetReplicasSnapshot(string keyspaceName, byte[] partitionKey)
        {
            return InternalMetadata.GetReplicas(keyspaceName, partitionKey);
        }

        /// <inheritdoc />
        public ICollection<Host> GetReplicasSnapshot(byte[] partitionKey)
        {
            return GetReplicasSnapshot(null, partitionKey);
        }

        private ClusterDescription GetClusterDescriptionInternal()
        {
            return new ClusterDescription(
                InternalMetadata.ClusterName, InternalMetadata.IsDbaas, InternalMetadata.ProtocolVersion);
        }

        /// <inheritdoc />
        public Host GetHost(IPEndPoint address)
        {
            TryInitialize();
            return InternalMetadata.GetHost(address);
        }

        /// <inheritdoc />
        public async Task<Host> GetHostAsync(IPEndPoint address)
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return InternalMetadata.GetHost(address);
        }

        /// <inheritdoc />
        public ICollection<Host> AllHosts()
        {
            TryInitialize();
            return InternalMetadata.AllHosts();
        }

        /// <inheritdoc />
        public async Task<ICollection<Host>> AllHostsAsync()
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return InternalMetadata.AllHosts();
        }

        /// <inheritdoc />
        public IEnumerable<IPEndPoint> AllReplicas()
        {
            TryInitialize();
            return InternalMetadata.AllReplicas();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IPEndPoint>> AllReplicasAsync()
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return InternalMetadata.AllReplicas();
        }

        /// <inheritdoc />
        public ICollection<Host> GetReplicas(string keyspaceName, byte[] partitionKey)
        {
            TryInitialize();
            return InternalMetadata.GetReplicas(keyspaceName, partitionKey);
        }

        /// <inheritdoc />
        public ICollection<Host> GetReplicas(byte[] partitionKey)
        {
            return GetReplicas(null, partitionKey);
        }

        /// <inheritdoc />
        public async Task<ICollection<Host>> GetReplicasAsync(string keyspaceName, byte[] partitionKey)
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return InternalMetadata.GetReplicas(keyspaceName, partitionKey);
        }

        /// <inheritdoc />
        public Task<ICollection<Host>> GetReplicasAsync(byte[] partitionKey)
        {
            return GetReplicasAsync(null, partitionKey);
        }

        /// <inheritdoc />
        public KeyspaceMetadata GetKeyspace(string keyspace)
        {
            return TaskHelper.WaitToComplete(GetKeyspaceAsync(keyspace), _queryAbortTimeout);
        }

        /// <inheritdoc />
        public async Task<KeyspaceMetadata> GetKeyspaceAsync(string keyspace)
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return await InternalMetadata.GetKeyspaceAsync(keyspace).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public ICollection<string> GetKeyspaces()
        {
            return TaskHelper.WaitToComplete(GetKeyspacesAsync(), _queryAbortTimeout);
        }

        /// <inheritdoc />
        public async Task<ICollection<string>> GetKeyspacesAsync()
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return await InternalMetadata.GetKeyspacesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc />
        public ICollection<string> GetTables(string keyspace)
        {
            return TaskHelper.WaitToComplete(GetTablesAsync(keyspace), _queryAbortTimeout);
        }

        /// <inheritdoc />
        public async Task<ICollection<string>> GetTablesAsync(string keyspace)
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return await InternalMetadata.GetTablesAsync(keyspace).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public TableMetadata GetTable(string keyspace, string tableName)
        {
            return TaskHelper.WaitToComplete(GetTableAsync(keyspace, tableName), _queryAbortTimeout * 2);
        }

        /// <inheritdoc />
        public async Task<TableMetadata> GetTableAsync(string keyspace, string tableName)
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return await InternalMetadata.GetTableAsync(keyspace, tableName).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public MaterializedViewMetadata GetMaterializedView(string keyspace, string name)
        {
            return TaskHelper.WaitToComplete(GetMaterializedViewAsync(keyspace, name), _queryAbortTimeout * 2);
        }

        /// <inheritdoc />
        public async Task<MaterializedViewMetadata> GetMaterializedViewAsync(string keyspace, string name)
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return await InternalMetadata.GetMaterializedViewAsync(keyspace, name).ConfigureAwait(false);
        }
        
        /// <inheritdoc />
        public UdtColumnInfo GetUdtDefinition(string keyspace, string typeName)
        {
            return TaskHelper.WaitToComplete(GetUdtDefinitionAsync(keyspace, typeName), _queryAbortTimeout);
        }

        /// <inheritdoc />
        public async Task<UdtColumnInfo> GetUdtDefinitionAsync(string keyspace, string typeName)
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return await InternalMetadata.GetUdtDefinitionAsync(keyspace, typeName).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public FunctionMetadata GetFunction(string keyspace, string name, string[] signature)
        {
            return TaskHelper.WaitToComplete(GetFunctionAsync(keyspace, name, signature), _queryAbortTimeout);
        }

        /// <inheritdoc />
        public async Task<FunctionMetadata> GetFunctionAsync(string keyspace, string name, string[] signature)
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return await InternalMetadata.GetFunctionAsync(keyspace, name, signature).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public AggregateMetadata GetAggregate(string keyspace, string name, string[] signature)
        {
            return TaskHelper.WaitToComplete(GetAggregateAsync(keyspace, name, signature), _queryAbortTimeout);
        }

        /// <inheritdoc />
        public async Task<AggregateMetadata> GetAggregateAsync(string keyspace, string name, string[] signature)
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return await InternalMetadata.GetAggregateAsync(keyspace, name, signature).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public bool RefreshSchema(string keyspace = null, string table = null)
        {
            return TaskHelper.WaitToComplete(RefreshSchemaAsync(keyspace, table), _queryAbortTimeout * 2);
        }

        /// <inheritdoc />
        public async Task<bool> RefreshSchemaAsync(string keyspace = null, string table = null)
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return await InternalMetadata.RefreshSchemaAsync(keyspace, table).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> CheckSchemaAgreementAsync()
        {
            await TryInitializeAsync().ConfigureAwait(false);
            return await InternalMetadata.CheckSchemaAgreementAsync().ConfigureAwait(false);
        }

        internal Task TryInitializeAsync()
        {
            var currentState = Interlocked.Read(ref _state);
            if (currentState == Metadata.Initialized)
            {
                //It was already initialized
                return TaskHelper.Completed;
            }

            return InitializeAsync(currentState);
        }

        internal void TryInitialize()
        {
            var currentState = Interlocked.Read(ref _state);
            if (currentState == Metadata.Initialized)
            {
                //It was already initialized
                return;
            }

            TaskHelper.WaitToComplete(InitializeAsync(currentState), _queryAbortTimeout);
        }

        private Task InitializeAsync(long currentState)
        {
            if (currentState == Metadata.Disposed)
            {
                throw new ObjectDisposedException("This metadata object has been disposed.");
            }

            if (Interlocked.CompareExchange(ref _state, Metadata.Initializing, Metadata.NotInitialized)
                == Metadata.NotInitialized)
            {
                _initTask = Task.Run(InitializeInternalAsync);
            }

            return _initTask;
        }

        private async Task InitializeInternalAsync()
        {
            await InternalMetadata.InitAsync().ConfigureAwait(false);
            var previousState = Interlocked.CompareExchange(ref _state, Metadata.Initialized, Metadata.Initializing);
            if (previousState == Metadata.Disposed)
            {
                await InternalMetadata.ShutdownAsync().ConfigureAwait(false);
                throw new ObjectDisposedException("Metadata instance was disposed before initialization finished.");
            }
        }

        internal async Task ShutdownAsync()
        {
            var previousState = Interlocked.Exchange(ref _state, Metadata.Disposed);

            if (previousState != Metadata.Initialized)
            {
                return;
            }

            await InternalMetadata.ShutdownAsync().ConfigureAwait(false);
        }

        internal void OnHostRemoved(Host h)
        {
            HostRemoved?.Invoke(h);
        }

        internal void OnHostAdded(Host h)
        {
            HostAdded?.Invoke(h);
        }

        internal void FireSchemaChangedEvent(SchemaChangedEventArgs.Kind what, string keyspace, string table, object sender = null)
        {
            SchemaChangedEvent?.Invoke(sender ?? this, new SchemaChangedEventArgs { Keyspace = keyspace, What = what, Table = table });
        }

        internal void OnHostDown(Host h)
        {
            HostsEvent?.Invoke(this, new HostsEventArgs { Address = h.Address, What = HostsEventArgs.Kind.Down });
        }

        internal void OnHostUp(Host h)
        {
            HostsEvent?.Invoke(h, new HostsEventArgs { Address = h.Address, What = HostsEventArgs.Kind.Up });
        }
    }
}