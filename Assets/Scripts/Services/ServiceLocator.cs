using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tiny typed service locator for scene-static managers.
/// Prefer <c>LobbyManager.Instance</c> / <c>FirebaseExchanger.Instance</c> when available;
/// use this for optional cross-cutting lookups without <c>FindObjectOfType</c>.
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

    public static void Register<T>(T instance) where T : class
    {
        if (instance == null)
        {
            return;
        }

        Services[typeof(T)] = instance;
    }

    public static void Unregister<T>(T instance) where T : class
    {
        if (instance == null)
        {
            return;
        }

        object existing;
        if (Services.TryGetValue(typeof(T), out existing) && ReferenceEquals(existing, instance))
        {
            Services.Remove(typeof(T));
        }
    }

    public static T Get<T>() where T : class
    {
        object existing;
        if (Services.TryGetValue(typeof(T), out existing))
        {
            return existing as T;
        }

        return null;
    }

    public static T GetRequired<T>() where T : class
    {
        T service = Get<T>();
        if (service == null)
        {
            throw new InvalidOperationException(typeof(T).Name + " is not registered with ServiceLocator.");
        }

        return service;
    }
}
