using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

namespace Yamara
{
    public class WebTextureRequestSender : IEnumerable
    {
        public class Data
        {
            public readonly Texture2D Tex;
            public readonly UnityWebRequest.Result Result;
            public readonly string Error;
            public Data(Texture2D texture, UnityWebRequest request)
            {
                Tex = texture;
                Result = request.result;
                Error = request.error;
            }
        }

        public string UrlFormat;
        public Texture2D ErrorTexture;

        private readonly Dictionary<string, Subject<Data>> _subjects = new Dictionary<string, Subject<Data>>();
        private readonly Dictionary<string, UnityWebRequest> _requests = new Dictionary<string, UnityWebRequest>();
        private readonly Dictionary<string, IDisposable> _disposes = new Dictionary<string, IDisposable>();

        public WebTextureRequestSender(
            string urlFormat = "",
            Texture2D errorTexture = null)
        {
            UrlFormat = urlFormat;
            ErrorTexture = errorTexture;
        }

        public IEnumerable<string> Keys => _subjects.Keys;
        public IEnumerable<IObservable<Data>> Values => _subjects.Values;

        public int Count => _subjects.Count;

        public IObservable<Data> AddRequest(params string[] args) => AddRequest(null, args);
        public IObservable<Data> AddRequest(string arg0, Action<Data> resultData) => AddRequest(resultData, arg0);
        public IObservable<Data> AddRequest(string arg0, string arg1, Action<Data> resultData) => AddRequest(resultData, arg0, arg1);
        public IObservable<Data> AddRequest(string arg0, string arg1, string arg2, Action<Data> resultData) => AddRequest(resultData, arg0, arg1, arg2);
        public IObservable<Data> AddRequest(string arg0, string arg1, string arg2, string arg3, Action<Data> resultData) => AddRequest(resultData, arg0, arg1, arg2, arg3);
        public IObservable<Data> AddRequest(Action<Data> completed, params string[] args)
        {
            if (_subjects.ContainsKey(args[0]))
            {
                _subjects[args[0]].Subscribe(d => completed?.Invoke(d));
                return _subjects[args[0]];
            }

            var url =
                string.IsNullOrEmpty(UrlFormat)
                ? args[0]
                : string.Format(UrlFormat, args);

            var subject = new Subject<Data>();
            subject.Subscribe(d => completed?.Invoke(d));

            var request = UnityWebRequestTexture.GetTexture(url);
            var operation = request.SendWebRequest();

            _subjects.Add(args[0], subject);
            _requests.Add(args[0], request);

            IObservable<AsyncOperation> completedObservable;

            // Code to reproduce pseudo-lag.
            // completedObservable = GetCompletedObserverInPseudoLag(operation);
            completedObservable = GetCompletedObserver(operation);

            _disposes.Add(args[0], completedObservable.Subscribe(_ => OnCompletedProcess(request, args[0])));

            return _subjects[args[0]];
        }

        private IObservable<AsyncOperation> GetCompletedObserver(UnityWebRequestAsyncOperation operation)
            => Observable.FromEvent<AsyncOperation>(h => operation.completed += h, h => operation.completed -= h);

        private IObservable<AsyncOperation> GetCompletedObserverInPseudoLag(UnityWebRequestAsyncOperation operation)
        {
            var minLagRange = 1f;
            var maxLagRange = 2f;
            return Observable.FromEvent<AsyncOperation>(h => operation.completed += h, h => operation.completed -= h)
                .Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(minLagRange, maxLagRange)))
                .Select(asyncOperation =>
                {
                    Debug.Log("Reproducing pseudo lag.");
                    return asyncOperation;
                });
        }

        public IObservable<Data> GetObservable(string key) => _subjects[key];

        public void Clear() => _subjects.ToList().ForEach(subject => Remove(subject.Key));

        public bool ContainsKey(string arg0) => _subjects.ContainsKey(arg0);

        public bool ContainsValue(IObservable<Data> observable) => _subjects.Any(subject => subject.Value == observable);

        public void CopyTo(KeyValuePair<string, Subject<Data>>[] array, int arrayIndex)
        {
            if (arrayIndex >= _subjects.Count)
            {
                array = new KeyValuePair<string, Subject<Data>>[0];
                return;
            }
            array = new KeyValuePair<string, Subject<Data>>[_subjects.Count - arrayIndex];

            var list = _subjects.Skip(arrayIndex).ToList();
            Enumerable.Range(0, list.Count).ToList().ForEach(i => array[i] = list[i]);
        }

        public bool Remove(IObservable<Data> observable)
        {
            var pair =_subjects.FirstOrDefault(x => x.Value == observable);
            if (pair.Value == null) return false;
            return Remove(pair.Key);
        }

        public bool Remove(string arg0)
        {
            if (!_subjects.ContainsKey(arg0)) return false;

            _subjects[arg0].Dispose();
            _subjects.Remove(arg0);
            _requests[arg0].Dispose();
            _requests.Remove(arg0);
            _disposes[arg0].Dispose();
            _disposes.Remove(arg0);

            return true;
        }

        public bool TryGetValue(string arg0, out IObservable<Data> completed)
        {
            if (!_subjects.ContainsKey(arg0))
            {
                completed = default;
                return false;
            }
            completed = _subjects[arg0];
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator() => _subjects.GetEnumerator();

        private void OnCompletedProcess(UnityWebRequest request, string arg0)
        {
            var texture = ErrorTexture;

            if (request.result == UnityWebRequest.Result.Success)
            {
                texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            }

            _subjects[arg0].OnNext(new Data(texture, request));

            Remove(arg0);
        }
    }
}
