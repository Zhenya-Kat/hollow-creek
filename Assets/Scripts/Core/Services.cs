using System;
using System.Collections.Generic;
using UnityEngine;

namespace HollowCreek.Core
{
    /// <summary>
    /// Реестр глобальных сервисов игры (состояние, сохранения, загрузка локаций…).
    /// Сервисы регистрируются при старте сцены Bootstrap и доступны из любого места через <see cref="Get{T}"/>.
    /// </summary>
    public static class Services
    {
        static readonly Dictionary<Type, object> Registry = new();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (Registry.TryGetValue(typeof(T), out var existing) && !ReferenceEquals(existing, service))
                Debug.LogWarning($"[Services] {typeof(T).Name} уже зарегистрирован и будет заменён.");
            Registry[typeof(T)] = service;
        }

        /// <summary>Удаляет сервис, только если зарегистрирован именно этот экземпляр.</summary>
        public static void Unregister<T>(T service) where T : class
        {
            if (Registry.TryGetValue(typeof(T), out var existing) && ReferenceEquals(existing, service))
                Registry.Remove(typeof(T));
        }

        public static T Get<T>() where T : class
        {
            if (TryGet<T>(out var service)) return service;
            throw new InvalidOperationException(
                $"Сервис {typeof(T).Name} не зарегистрирован. Игра должна запускаться через сцену Bootstrap.");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Registry.TryGetValue(typeof(T), out var value))
            {
                service = (T)value;
                return true;
            }
            service = null;
            return false;
        }

        public static void Clear() => Registry.Clear();

        // Статические поля переживают вход в Play Mode, если отключена перезагрузка домена, — чистим явно.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Clear();
    }
}
