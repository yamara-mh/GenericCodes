using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniRx.Triggers
{
    public class NetworkRunnerCallbacksTrigger : MonoBehaviour, INetworkRunnerCallbacks
    {
        Subject<NetworkRunner> onConnectedToServer;
        public IObservable<NetworkRunner> OnConnectedToServer => onConnectedToServer ??= new Subject<NetworkRunner>();
        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
          => onConnectedToServer?.OnNext(runner);

        Subject<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)> onConnectFailed = new Subject<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)>();
        public IObservable<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)> OnConnectFailed => onConnectFailed ??= new Subject<(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)>();
        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
          => onConnectFailed?.OnNext((runner, remoteAddress, reason));

        Subject<(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)> onConnectRequest;
        public IObservable<(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)> OnConnectRequest => onConnectRequest ??= new Subject<(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)>();
        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
          => onConnectRequest?.OnNext((runner, request, token));

        Subject<(NetworkRunner runner, Dictionary<string, object> data)> onCustomAuthenticationResponse;
        public IObservable<(NetworkRunner runner, Dictionary<string, object> data)> OnCustomAuthenticationResponse => onCustomAuthenticationResponse ??= new Subject<(NetworkRunner runner, Dictionary<string, object> data)>();
        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
          => onCustomAuthenticationResponse?.OnNext((runner, data));

        Subject<NetworkRunner> onDisconnectedFromServer;
        public IObservable<NetworkRunner> OnDisconnectedFromServer => onDisconnectedFromServer ??= new Subject<NetworkRunner>();
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner)
            => onDisconnectedFromServer?.OnNext(runner);

        Subject<(NetworkRunner runner, HostMigrationToken hostMigrationToken)> onHostMigration;
        public IObservable<(NetworkRunner runner, HostMigrationToken hostMigrationToken)> OnHostMigration => onHostMigration ??= new Subject<(NetworkRunner runner, HostMigrationToken hostMigrationToken)>();
        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
           => onHostMigration?.OnNext((runner, hostMigrationToken));

        Subject<(NetworkRunner runner, NetworkInput input)> onInput;
        public IObservable<(NetworkRunner runner, NetworkInput input)> OnInput => onInput ??= new Subject<(NetworkRunner runner, NetworkInput input)>();
        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
          => onInput?.OnNext((runner, input));

        Subject<(NetworkRunner runner, PlayerRef player, NetworkInput input)> onInputMissing;
        public IObservable<(NetworkRunner runner, PlayerRef player, NetworkInput input)> OnInputMissing => onInputMissing ??= new Subject<(NetworkRunner runner, PlayerRef player, NetworkInput input)>();
        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
          => onInputMissing?.OnNext((runner, player, input));

        Subject<(NetworkRunner runner, PlayerRef player)> onPlayerJoined;
        public IObservable<(NetworkRunner runner, PlayerRef player)> OnPlayerJoined => onPlayerJoined ??= new Subject<(NetworkRunner runner, PlayerRef player)>();
        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
          => onPlayerJoined?.OnNext((runner, player));

        Subject<(NetworkRunner runner, PlayerRef player)> onPlayerLeft;
        public IObservable<(NetworkRunner runner, PlayerRef player)> OnPlayerLeft => onPlayerLeft ??= new Subject<(NetworkRunner runner, PlayerRef player)>();
        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
          => onPlayerLeft?.OnNext((runner, player));

        Subject<(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)> onReliableDataReceived;
        public IObservable<(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)> OnReliableDataReceived => onReliableDataReceived ??= new Subject<(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)>();
        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data)
          => onReliableDataReceived?.OnNext((runner, player, data));

        Subject<NetworkRunner> onSceneLoadDone;
        public IObservable<NetworkRunner> OnSceneLoadDone => onSceneLoadDone ??= new Subject<NetworkRunner>();
        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
          => onSceneLoadDone?.OnNext(runner);

        Subject<NetworkRunner> onSceneLoadStart;
        public IObservable<NetworkRunner> OnSceneLoadStart => onSceneLoadStart ??= new Subject<NetworkRunner>();
        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
          => onSceneLoadStart?.OnNext(runner);

        Subject<(NetworkRunner runner, List<SessionInfo> sessionList)> onSessionListUpdated;
        public IObservable<(NetworkRunner runner, List<SessionInfo> sessionList)> OnSessionListUpdated => onSessionListUpdated ??= new Subject<(NetworkRunner runner, List<SessionInfo> sessionList)>();
        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
          => onSessionListUpdated?.OnNext((runner, sessionList));

        Subject<(NetworkRunner runner, ShutdownReason shutdownReason)> onShutdown;
        public IObservable<(NetworkRunner runner, ShutdownReason shutdownReason)> OnShutdown => onShutdown ??= new Subject<(NetworkRunner runner, ShutdownReason shutdownReason)>();
        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
            => onShutdown?.OnNext((runner, shutdownReason));

        Subject<(NetworkRunner runner, SimulationMessagePtr message)> onUserSimulationMessage;
        public IObservable<(NetworkRunner runner, SimulationMessagePtr message)> OnUserSimulationMessage => onUserSimulationMessage ??= new Subject<(NetworkRunner runner, SimulationMessagePtr message)>();
        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
          => onUserSimulationMessage?.OnNext((runner, message));
    }
}
