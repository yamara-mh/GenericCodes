using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;

namespace UniRx.Triggers
{
    public static class NetworkRunnerTriggerExtensions
    {
        public static NetworkRunnerEventTrigger GetOrAddComponent(NetworkRunner runner)
        {
            NetworkRunnerEventTrigger eventTrigger;
            if (runner.TryGetComponent(out eventTrigger)) return eventTrigger;
            eventTrigger = runner.gameObject.AddComponent<NetworkRunnerEventTrigger>();
            runner.AddCallbacks(eventTrigger);
            return eventTrigger;
        }

        public static IObservable<NetworkRunner> OnConnectedToServer(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<NetworkRunner>();
            return GetOrAddComponent(runner).OnConnectedToServer;
        }

        public static IObservable<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)> OnConnectFailed(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)>();
            return GetOrAddComponent(runner).OnConnectFailed;
        }

        public static IObservable<(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)> OnConnectRequest(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)>();
            return GetOrAddComponent(runner).OnConnectRequest;
        }

        public static IObservable<(NetworkRunner runner, Dictionary<string, object> data)> OnCustomAuthenticationResponse(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, Dictionary<string, object> data)>();
            return GetOrAddComponent(runner).OnCustomAuthenticationResponse;
        }

        public static IObservable<NetworkRunner> OnDisconnectedFromServer(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<NetworkRunner>();
            return GetOrAddComponent(runner).OnDisconnectedFromServer;
        }

        public static IObservable<(NetworkRunner runner, HostMigrationToken hostMigrationToken)> OnHostMigration(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, HostMigrationToken hostMigrationToken)>();
            return GetOrAddComponent(runner).OnHostMigration;
        }

        public static IObservable<(NetworkRunner runner, NetworkInput input)> OnInput(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, NetworkInput input)>();
            return GetOrAddComponent(runner).OnInput;
        }

        public static IObservable<(NetworkRunner runner, PlayerRef player, NetworkInput input)> OnInputMissing(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, PlayerRef player, NetworkInput input)>();
            return GetOrAddComponent(runner).OnInputMissing;
        }

        public static IObservable<(NetworkRunner runner, PlayerRef player)> OnPlayerJoined(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, PlayerRef player)>();
            return GetOrAddComponent(runner).OnPlayerJoined;
        }

        public static IObservable<(NetworkRunner runner, PlayerRef player)> OnPlayerLeft(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, PlayerRef player)>();
            return GetOrAddComponent(runner).OnPlayerLeft;
        }

        public static IObservable<(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)> OnReliableDataReceived(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)>();
            return GetOrAddComponent(runner).OnReliableDataReceived;
        }

        public static IObservable<NetworkRunner> OnSceneLoadDone(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<NetworkRunner>();
            return GetOrAddComponent(runner).OnSceneLoadDone;
        }

        public static IObservable<NetworkRunner> OnSceneLoadStart(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<NetworkRunner>();
            return GetOrAddComponent(runner).OnSceneLoadStart;
        }

        public static IObservable<(NetworkRunner runner, List<SessionInfo> sessionList)> OnSessionListUpdated(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, List<SessionInfo> sessionList)>();
            return GetOrAddComponent(runner).OnSessionListUpdated;
        }

        public static IObservable<(NetworkRunner runner, ShutdownReason shutdownReason)> OnShutdown(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, ShutdownReason shutdownReason)>();
            return GetOrAddComponent(runner).OnShutdown;
        }

        public static IObservable<(NetworkRunner runner, SimulationMessagePtr message)> OnUserSimulationMessage(this NetworkRunner runner)
        {
            if (runner == null || runner.gameObject == null) return Observable.Empty<(NetworkRunner runner, SimulationMessagePtr message)>();
            return GetOrAddComponent(runner).OnUserSimulationMessage;
        }
    }
}
