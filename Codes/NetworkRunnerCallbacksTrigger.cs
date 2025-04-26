using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace R3.Triggers
{
    public class NetworkRunnerCallbacksTrigger : MonoBehaviour, INetworkRunnerCallbacks
    {
        Subject<NetworkRunner> onConnectedToServer;
        public Observable<NetworkRunner> OnConnectedToServer => onConnectedToServer ??= new Subject<NetworkRunner>();
        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
          => onConnectedToServer?.OnNext(runner);

        Subject<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)> onConnectFailed = new Subject<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)>();
        public Observable<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)> OnConnectFailed => onConnectFailed ??= new Subject<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)>();
        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
          => onConnectFailed?.OnNext((runner, remoteAddress, reason));

        Subject<(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)> onConnectRequest;
        public Observable<(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)> OnConnectRequest => onConnectRequest ??= new Subject<(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)>();
        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
          => onConnectRequest?.OnNext((runner, request, token));

        Subject<(NetworkRunner runner, Dictionary<string, object> data)> onCustomAuthenticationResponse;
        public Observable<(NetworkRunner runner, Dictionary<string, object> data)> OnCustomAuthenticationResponse => onCustomAuthenticationResponse ??= new Subject<(NetworkRunner runner, Dictionary<string, object> data)>();
        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
          => onCustomAuthenticationResponse?.OnNext((runner, data));

        Subject<(NetworkRunner runner, NetDisconnectReason reason)> onDisconnectedFromServer;
        public Observable<(NetworkRunner runner, NetDisconnectReason reason)> OnDisconnectedFromServer => onDisconnectedFromServer ??= new Subject<(NetworkRunner runner, NetDisconnectReason reason)>();
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
            => onDisconnectedFromServer?.OnNext((runner, reason));

        Subject<(NetworkRunner runner, HostMigrationToken hostMigrationToken)> onHostMigration;
        public Observable<(NetworkRunner runner, HostMigrationToken hostMigrationToken)> OnHostMigration => onHostMigration ??= new Subject<(NetworkRunner runner, HostMigrationToken hostMigrationToken)>();
        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
           => onHostMigration?.OnNext((runner, hostMigrationToken));

        Subject<(NetworkRunner runner, NetworkInput input)> onInput;
        public Observable<(NetworkRunner runner, NetworkInput input)> OnInput => onInput ??= new Subject<(NetworkRunner runner, NetworkInput input)>();
        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
          => onInput?.OnNext((runner, input));

        Subject<(NetworkRunner runner, PlayerRef player, NetworkInput input)> onInputMissing;
        public Observable<(NetworkRunner runner, PlayerRef player, NetworkInput input)> OnInputMissing => onInputMissing ??= new Subject<(NetworkRunner runner, PlayerRef player, NetworkInput input)>();
        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
          => onInputMissing?.OnNext((runner, player, input));

        Subject<(NetworkRunner runner, PlayerRef player)> onPlayerJoined;
        public Observable<(NetworkRunner runner, PlayerRef player)> OnPlayerJoined => onPlayerJoined ??= new Subject<(NetworkRunner runner, PlayerRef player)>();
        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
          => onPlayerJoined?.OnNext((runner, player));

        Subject<(NetworkRunner runner, PlayerRef player)> onPlayerLeft;
        public Observable<(NetworkRunner runner, PlayerRef player)> OnPlayerLeft => onPlayerLeft ??= new Subject<(NetworkRunner runner, PlayerRef player)>();
        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
          => onPlayerLeft?.OnNext((runner, player));

        Subject<(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)> onReliableDataReceived;
        public Observable<(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)> OnReliableDataReceived => onReliableDataReceived ??= new Subject<(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)>();
        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
          => onReliableDataReceived?.OnNext((runner, player, key, data));

        Subject<NetworkRunner> onSceneLoadDone;
        public Observable<NetworkRunner> OnSceneLoadDone => onSceneLoadDone ??= new Subject<NetworkRunner>();
        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
          => onSceneLoadDone?.OnNext(runner);

        Subject<NetworkRunner> onSceneLoadStart;
        public Observable<NetworkRunner> OnSceneLoadStart => onSceneLoadStart ??= new Subject<NetworkRunner>();
        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
          => onSceneLoadStart?.OnNext(runner);

        Subject<(NetworkRunner runner, List<SessionInfo> sessionList)> onSessionListUpdated;
        public Observable<(NetworkRunner runner, List<SessionInfo> sessionList)> OnSessionListUpdated => onSessionListUpdated ??= new Subject<(NetworkRunner runner, List<SessionInfo> sessionList)>();
        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
          => onSessionListUpdated?.OnNext((runner, sessionList));

        Subject<(NetworkRunner runner, ShutdownReason shutdownReason)> onShutdown;
        public Observable<(NetworkRunner runner, ShutdownReason shutdownReason)> OnShutdown => onShutdown ??= new Subject<(NetworkRunner runner, ShutdownReason shutdownReason)>();
        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
            => onShutdown?.OnNext((runner, shutdownReason));

        Subject<(NetworkRunner runner, SimulationMessagePtr message)> onUserSimulationMessage;
        public Observable<(NetworkRunner runner, SimulationMessagePtr message)> OnUserSimulationMessage => onUserSimulationMessage ??= new Subject<(NetworkRunner runner, SimulationMessagePtr message)>();
        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
          => onUserSimulationMessage?.OnNext((runner, message));


        Subject<(NetworkRunner runner, NetworkObject obj, PlayerRef player)> onObjectExitAOI;
        public Observable<(NetworkRunner runner, NetworkObject obj, PlayerRef player)> OnObjectExitAOI => onObjectExitAOI ??= new Subject<(NetworkRunner runner, NetworkObject obj, PlayerRef player)>();
        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
          => onObjectExitAOI?.OnNext((runner, obj, player));

        Subject<(NetworkRunner runner, NetworkObject obj, PlayerRef player)> onObjectEnterAOI;
        public Observable<(NetworkRunner runner, NetworkObject obj, PlayerRef player)> OnObjectEnterAOI => onObjectEnterAOI ??= new Subject<(NetworkRunner runner, NetworkObject obj, PlayerRef player)>();
        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
          => onObjectEnterAOI?.OnNext((runner, obj, player));

        Subject<(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)> onReliableDataProgress;
        public Observable<(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)> OnReliableDataProgress => onReliableDataProgress ??= new Subject<(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)>();
        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
          => onReliableDataProgress?.OnNext((runner, player, key, progress));
    }
}
