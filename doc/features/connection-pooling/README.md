# Connection pooling

For each host selected by the load-balancing policy, the driver keeps a core amount of connections open at all times
(`GetCoreConnectionsPerHost(HostDistance)`).

If the use of those connections reaches a configurable threshold 
(`GetMaxSimultaneousRequestsPerConnectionTreshold(HostDistance)`), more connections are created up to the
configurable maximum number of connections (`GetMaxConnectionPerHost(HostDistance)`).

The driver uses connections in an asynchronous manner and multiple requests can be submitted on the same connection
at the same time without waiting for a response. This means that the driver only needs to maintain a relatively small
number of connections to each Cassandra host. The [PoolingOptions][pooling-options-api] allows you to control
how many connections are kept per host.

Each of these parameters can be separately set for `Local` and `Remote` hosts. For `Ignored` hosts, the default for
all those settings is `0` and cannot be changed.

The default amount of connections depends on the Cassandra version of your cluster, because newer versions of
Cassandra (2.1 and above) support a higher number of in-flight requests.

For Cassandra 2.1 and above, the default amount of connections per host is:

- Local datacenter: one core connection per host, with two connections as maximum if the simultaneous requests
threshold is reached.
- Remote datacenter: one core connection per host (being one also max).

For older Cassandra versions (1.2 and 2.0), the default amount of connections per host are:

- Local datacenter: two core connection per host, with eight connections as maximum if the simultaneous requests
threshold is reached.
- Remote datacenter: one core connection per host (being two the maximum).

[pooling-options-api]: http://docs.datastax.com/en/latest-csharp-driver-api/html/T_Cassandra_PoolingOptions.htm