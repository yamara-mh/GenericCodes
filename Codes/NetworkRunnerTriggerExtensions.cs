using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

namespace R3.Triggers
{
    public static class NetworkRunnerTriggerExtensions
    {
        public static NetworkRunnerCallbacksTrigger GetOrAddComponent(NetworkRunner runner)
        {
            NetworkRunnerCallbacksTrigger eventTrigger;
            if (runner.TryGetComponent(out eventTrigger)) return eventTrigger;
            eventTrigger = runner.gameObject.AddComponent<NetworkRunnerCallbacksTrigger>();
            runner.AddCallbacks(eventTrigger);
            return eventTrigger;
        }

        public static Observable<NetworkRunner> OnConnectedToServer(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<NetworkRunner>();
            return GetOrAddComponent(runner).OnConnectedToServer;
        }

        public static Observable<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)> OnConnectFailed(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)>();
            return GetOrAddComponent(runner).OnConnectFailed;
        }

        public static Observable<(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)> OnConnectRequest(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)>();
            return GetOrAddComponent(runner).OnConnectRequest;
        }

        public static Observable<(NetworkRunner runner, Dictionary<string, object> data)> OnCustomAuthenticationResponse(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, Dictionary<string, object> data)>();
            return GetOrAddComponent(runner).OnCustomAuthenticationResponse;
        }

        public static Observable<(NetworkRunner runner, NetDisconnectReason reason)> OnDisconnectedFromServer(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, NetDisconnectReason reason)>();
            return GetOrAddComponent(runner).OnDisconnectedFromServer;
        }

        public static Observable<(NetworkRunner runner, HostMigrationToken hostMigrationToken)> OnHostMigration(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, HostMigrationToken hostMigrationToken)>();
            return GetOrAddComponent(runner).OnHostMigration;
        }

        public static Observable<(NetworkRunner runner, NetworkInput input)> OnInput(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, NetworkInput input)>();
            return GetOrAddComponent(runner).OnInput;
        }

        public static Observable<(NetworkRunner runner, PlayerRef player, NetworkInput input)> OnInputMissing(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, PlayerRef player, NetworkInput input)>();
            return GetOrAddComponent(runner).OnInputMissing;
        }

        public static Observable<(NetworkRunner runner, PlayerRef player)> OnPlayerJoined(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, PlayerRef player)>();
            return GetOrAddComponent(runner).OnPlayerJoined;
        }

        public static Observable<(NetworkRunner runner, PlayerRef player)> OnPlayerLeft(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, PlayerRef player)>();
            return GetOrAddComponent(runner).OnPlayerLeft;
        }

        public static Observable<(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)> OnReliableDataReceived(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)>();
            return GetOrAddComponent(runner).OnReliableDataReceived;
        }

        public static Observable<NetworkRunner> OnSceneLoadDone(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<NetworkRunner>();
            return GetOrAddComponent(runner).OnSceneLoadDone;
        }

        public static Observable<NetworkRunner> OnSceneLoadStart(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<NetworkRunner>();
            return GetOrAddComponent(runner).OnSceneLoadStart;
        }

        public static Observable<(NetworkRunner runner, List<SessionInfo> sessionList)> OnSessionListUpdated(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, List<SessionInfo> sessionList)>();
            return GetOrAddComponent(runner).OnSessionListUpdated;
        }

        public static Observable<(NetworkRunner runner, ShutdownReason shutdownReason)> OnShutdown(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, ShutdownReason shutdownReason)>();
            return GetOrAddComponent(runner).OnShutdown;
        }

        public static Observable<(NetworkRunner runner, SimulationMessagePtr message)> OnUserSimulationMessage(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, SimulationMessagePtr message)>();
            return GetOrAddComponent(runner).OnUserSimulationMessage;
        }
    }
}
