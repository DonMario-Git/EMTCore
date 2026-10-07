using System;
using System.Reflection;
using UnityEngine;

namespace EMT
{
    /// <summary>Interfaz marcadora. No exige implementar nada.</summary>
    public interface ISingleton { }

    /// <summary>Opcional: configura el comportamiento sin tocar el código de la clase.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SingletonOptionsAttribute : Attribute
    {
        /// <summary>MonoBehaviour creado automáticamente: sobrevive entre escenas.</summary>
        public bool Persist = true;
        /// <summary>MonoBehaviour creado automáticamente: oculto en la jerarquía.</summary>
        public bool HideInHierarchy = true;
        /// <summary>ScriptableObject: ruta dentro de Resources (si es null usa el nombre del tipo).</summary>
        public string ResourcesPath = null;
    }

    /// <summary>Acceso y creación perezosa de la instancia única de T.</summary>
    public static class Singleton<T> where T : class, ISingleton
    {
        static readonly SingletonOptionsAttribute DefaultOptions = new();
        static readonly bool IsUnityObj = typeof(UnityEngine.Object).IsAssignableFrom(typeof(T));

        static T _instance;
        static bool _hooked;

        /// <summary>Instancia única. Se crea o busca la primera vez que se pide.</summary>
        public static T Instance
        {
            get
            {
                var i = _instance;
                if (i != null && (!IsUnityObj || (i as UnityEngine.Object))) return i;
                return _instance = Create();
            }
        }

        /// <summary>¿Existe ya una instancia? No la crea.</summary>
        public static bool Exists
        {
            get
            {
                var i = _instance;
                return i != null && (!IsUnityObj || (i as UnityEngine.Object));
            }
        }

        /// <summary>Registra manualmente una instancia ya construida (útil para inyección o tests).</summary>
        public static void Register(T instance)
        {
            Hook();
            _instance = instance;
        }

        /// <summary>Libera la instancia (y destruye el objeto si es de Unity).</summary>
        public static void Clear()
        {
            if (IsUnityObj)
            {
                var obj = _instance as UnityEngine.Object;
                if (obj)
                {
                    if (obj is Component c) UnityEngine.Object.Destroy(c.gameObject);
                    else UnityEngine.Object.Destroy(obj);
                }
            }
            else if (_instance is IDisposable d) d.Dispose();

            _instance = null;
        }

        // ---------------------------------------------------------------

        static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            SingletonRuntime.OnReset += ResetState;
        }

        static void ResetState()
        {
            _instance = null;
            _hooked = false;
        }

        static T Create()
        {
            if (SingletonRuntime.Quitting) return null;
            Hook();

            Type type = typeof(T);
            var opt = type.GetCustomAttribute<SingletonOptionsAttribute>(false) ?? DefaultOptions;

            if (typeof(MonoBehaviour).IsAssignableFrom(type)) return CreateComponent(type, opt);
            if (typeof(ScriptableObject).IsAssignableFrom(type)) return CreateScriptable(type, opt);

            // Clase C# normal: sin GameObject, admite constructor privado.
            return (T)Activator.CreateInstance(type, true);
        }

        static T CreateComponent(Type type, SingletonOptionsAttribute opt)
        {
            // 1) Reutiliza una instancia ya puesta en escena.
#if UNITY_2023_1_OR_NEWER
            var found = UnityEngine.Object.FindAnyObjectByType(type);
#else
            var found = UnityEngine.Object.FindObjectOfType(type);
#endif
            if (found != null) return found as T;

            if (!Application.isPlaying)
            {
                Debug.LogWarning($"[Singleton] No se crea {type.Name} fuera de Play Mode.");
                return null;
            }

            // 2) Un componente necesita un GameObject: se crea uno oculto.
            var go = new GameObject($"[{type.Name}]")
            {
                hideFlags = opt.HideInHierarchy ? HideFlags.HideInHierarchy : HideFlags.None
            };
            if (opt.Persist) UnityEngine.Object.DontDestroyOnLoad(go);
            return go.AddComponent(type) as T;
        }

        static T CreateScriptable(Type type, SingletonOptionsAttribute opt)
        {
            // 1) Asset en Resources, 2) si no existe, instancia en memoria (sin GameObject).
            string path = string.IsNullOrEmpty(opt.ResourcesPath) ? type.Name : opt.ResourcesPath;
            var asset = Resources.Load(path, type);
            if (asset != null) return asset as T;

            var so = ScriptableObject.CreateInstance(type);
            so.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return so as T;
        }
    }

    /// <summary>Gestiona el estado global (salida de la app y Domain Reload desactivado).</summary>
    static class SingletonRuntime
    {
        public static bool Quitting { get; private set; }
        public static event Action OnReset;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Init()
        {
            Quitting = false;

            // Copia y limpia antes de invocar: cada tipo se vuelve a suscribir (_hooked = false).
            var handlers = OnReset;
            OnReset = null;
            handlers?.Invoke();

            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;
        }

        static void OnQuit() => Quitting = true;
    }
}