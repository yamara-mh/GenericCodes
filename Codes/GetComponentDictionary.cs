// #define GetComponentDictionary_USE_UNITASK
// #define GetComponentDictionary_USE_UNIRX

using System.Collections.Generic;
using UnityEngine;


#if GetComponentDictionary_USE_UNITASK
using Cysharp.Threading.Tasks.Triggers;
#endif

#if GetComponentDictionary_USE_UNIRX
using UniRx;
using UniRx.Triggers;
#endif

namespace Generic
{
    public class GetComponentDictionary<C> : Dictionary<GameObject, C> where C : Component
    {
        private static StaticDictionary<GameObject, C> instance = null;
        public static StaticDictionary<GameObject, C> Instance
        {
            get
            {
                if (instance == null)
                {
#if UNITY_EDITOR
                    Application.quitting += ApplicationQuittingEditor;
#endif
                    instance = new StaticDictionary<GameObject, C>();
                }
                return instance;
            }
        }

        public static bool Contains(C component) => Instance.ContainsValue(component);


#if GetComponentDictionary_USE_UNITASK
        public static async void Add(C component, bool removeOnDestroyGameObject = true)
        {
            Instance.Add(component.gameObject, component);
            if (removeOnDestroyGameObject == false) return;
            var gameObject = component.gameObject;
            await gameObject.OnDestroyAsync();
            TryRemove(gameObject);
        }
#elif GetComponentDictionary_USE_UNIRX
        public static void Add(C component, bool removeOnDestroyGameObject = true)
        {
            Instance.Add(component.gameObject, component);
            if (removeOnDestroyGameObject == false) return;
            var gameObject = component.gameObject;
            gameObject.OnDestroyAsObservable().Subscribe(_ => TryRemove(gameObject));
        }
#else
        public static void Add(C component) => Instance.Add(component.gameObject, component);
#endif
        public static bool TryAdd(C component) => Instance.TryAdd(component.gameObject, component);

        public static new bool TryGetValue(GameObject gameObject, out C component) => Instance.TryGetValue(gameObject, out component);
        public static void Remove(C component) => Instance.Remove(component.gameObject);
        public static bool TryRemove(C component)
        {
            if (component == null) return false;
            return TryRemove(component.gameObject);
        }
        public static bool TryRemove(GameObject gameObject)
        {
            if (gameObject == null || instance == null) return false;
            instance.Remove(gameObject);
            return true;
        }

        public static void Dispose() => instance = null;

#if UNITY_EDITOR
        private static void ApplicationQuittingEditor()
        {
            Dispose();
            Application.quitting -= ApplicationQuittingEditor;
        }
#endif
    }
}
