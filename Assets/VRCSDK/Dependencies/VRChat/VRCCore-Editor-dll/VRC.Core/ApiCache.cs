using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VRC.Core
{
	public class ApiCache : MonoBehaviour
	{
		public class CacheEntry
		{
			public ApiCacheObject obj;

			public float time;
		}

		public class CachedResponse
		{
			public byte[] Data;

			public float Timestamp;

			public float Lifetime;

			public string DataAsText
			{
				get
				{
					if (Data == null)
					{
						return string.Empty;
					}
					return Encoding.UTF8.GetString(Data, 0, Data.Length);
				}
			}

			public CachedResponse(byte[] byteData, float timestamp, float lifetime)
			{
				Data = byteData;
				Timestamp = timestamp;
				Lifetime = lifetime;
			}
		}

		private static Dictionary<int, CachedResponse> apiResponseCache = new Dictionary<int, CachedResponse>();

		public static Dictionary<Type, Dictionary<string, CacheEntry>> cache = new Dictionary<Type, Dictionary<string, CacheEntry>>();

		public ApiCache()
			: this()
		{
		}

		public static void Init(GameObject obj)
		{
			obj.AddComponent<ApiCache>();
		}

		public static CachedResponse GetOrClearCachedResponse(string apiRequestPathAndQuery, float cacheLifetime = 3600f)
		{
			int hashCode = apiRequestPathAndQuery.GetHashCode();
			if (apiResponseCache.TryGetValue(hashCode, out CachedResponse value))
			{
				if (Time.get_realtimeSinceStartup() - value.Timestamp > Mathf.Min(value.Lifetime, cacheLifetime))
				{
					apiResponseCache.Remove(hashCode);
					return null;
				}
				return value;
			}
			return null;
		}

		public static void CacheResponse(string apiRequestPathAndQuery, byte[] data)
		{
			int hashCode = apiRequestPathAndQuery.GetHashCode();
			apiResponseCache[hashCode] = new CachedResponse(data, Time.get_realtimeSinceStartup(), 3600f);
		}

		public static void ClearResponseCache()
		{
			apiResponseCache.Clear();
		}

		public static bool Contains<T>(string id) where T : class, ApiCacheObject
		{
			return Contains(typeof(T), id);
		}

		public static bool Contains(Type t, string id)
		{
			if (!cache.ContainsKey(t))
			{
				return false;
			}
			return cache[t].ContainsKey(id);
		}

		public static bool Fetch<T>(string id, ref T target, float maxCacheAge = 3600f) where T : class, ApiCacheObject
		{
			return Fetch(typeof(T), id, ref target, maxCacheAge);
		}

		public static bool Fetch<T>(Type t, string id, ref T target, float maxCacheAge = 3600f) where T : class, ApiCacheObject
		{
			if (!typeof(T).IsAssignableFrom(t) || !TestFetch(t, id, target, maxCacheAge))
			{
				return false;
			}
			Debug.Log((object)("<color=cyan>Fetched " + t.Name + " with id " + id + " from cache.</color>"));
			target = (cache[t][id].obj as T);
			return true;
		}

		private static bool TestFetch(Type t, string id, ApiCacheObject target, float maxCacheAge = 3600f)
		{
			if (t == null || target == null || string.IsNullOrEmpty(id))
			{
				return false;
			}
			if (!t.IsAssignableFrom(target.GetType()))
			{
				return false;
			}
			if (!cache.ContainsKey(t))
			{
				return false;
			}
			Dictionary<string, CacheEntry> dictionary = cache[t];
			if (!dictionary.ContainsKey(id))
			{
				return false;
			}
			if (dictionary[id].obj == null || !dictionary[id].obj.GetType().Equals(t) || (maxCacheAge > 0f && Time.get_realtimeSinceStartup() - dictionary[id].time > maxCacheAge))
			{
				Invalidate(t, id);
				return false;
			}
			return true;
		}

		public static bool Save(string id, ApiCacheObject obj, bool andClone = false)
		{
			if (!obj.ShouldCache())
			{
				return false;
			}
			Type type = obj.GetType();
			if (!cache.ContainsKey(type))
			{
				cache.Add(type, new Dictionary<string, CacheEntry>());
			}
			Dictionary<string, CacheEntry> dictionary = cache[type];
			if (!dictionary.ContainsKey(id))
			{
				dictionary.Add(id, new CacheEntry
				{
					obj = obj,
					time = Time.get_realtimeSinceStartup()
				});
			}
			else
			{
				dictionary[id].time = Time.get_realtimeSinceStartup();
				dictionary[id].obj = obj;
			}
			if (andClone)
			{
				Save(id + "_copy", obj.Clone());
			}
			return true;
		}

		public static void Invalidate<T>(string id) where T : class, ApiCacheObject
		{
			RemoveWhere((string _id, CacheEntry entry) => _id.StartsWith(id) && entry.obj.GetType() == typeof(T));
		}

		public static void Invalidate(Type t, string id)
		{
			RemoveWhere((string _id, CacheEntry entry) => _id.StartsWith(id) && entry.obj.GetType() == t);
		}

		private void LateUpdate()
		{
			RemoveWhere((string id, CacheEntry entry) => Time.get_realtimeSinceStartup() - entry.time > entry.obj.GetLifeSpan());
		}

		private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
		{
			RemoveWhere((string id, CacheEntry entry) => entry.obj.ShouldClearOnLevelLoad());
		}

		private static void RemoveWhere(Func<string, CacheEntry, bool> predicate)
		{
			foreach (KeyValuePair<Type, Dictionary<string, CacheEntry>> item in cache)
			{
				Dictionary<string, CacheEntry> value = item.Value;
				string[] array = value.Keys.ToArray();
				string[] array2 = array;
				foreach (string text in array2)
				{
					CacheEntry arg = value[text];
					if (predicate(text, arg))
					{
						value.Remove(text);
					}
				}
			}
		}
	}
}