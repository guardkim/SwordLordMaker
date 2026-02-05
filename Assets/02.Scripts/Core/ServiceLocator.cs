using System;
using System.Collections.Generic;
using UnityEngine;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();
    private static bool _isInitialized;

    public static event Action OnServicesReady;

    public static void Register<T>(T service) where T : class
    {
        Type type = typeof(T);
        if (_services.ContainsKey(type))
        {
            Debug.LogWarning($"[ServiceLocator] Service {type.Name} already registered. Replacing.");
            _services[type] = service;
        }
        else
        {
            _services.Add(type, service);
            Debug.Log($"[ServiceLocator] Registered: {type.Name}");
        }
    }

    public static void Unregister<T>() where T : class
    {
        Type type = typeof(T);
        if (_services.ContainsKey(type))
        {
            _services.Remove(type);
            Debug.Log($"[ServiceLocator] Unregistered: {type.Name}");
        }
    }

    public static T Resolve<T>() where T : class
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out object service))
        {
            return service as T;
        }

        Debug.LogWarning($"[ServiceLocator] Service {type.Name} not registered.");
        return null;
    }

    public static bool TryResolve<T>(out T service) where T : class
    {
        Type type = typeof(T);
        if (_services.TryGetValue(type, out object obj))
        {
            service = obj as T;
            return service != null;
        }

        service = null;
        return false;
    }

    public static bool IsRegistered<T>() where T : class
    {
        return _services.ContainsKey(typeof(T));
    }

    public static void Clear()
    {
        _services.Clear();
        _isInitialized = false;
        Debug.Log("[ServiceLocator] Cleared all services.");
    }

    public static void MarkAsReady()
    {
        _isInitialized = true;
        OnServicesReady?.Invoke();
        Debug.Log("[ServiceLocator] Services ready.");
    }

    public static bool IsReady => _isInitialized;
}
