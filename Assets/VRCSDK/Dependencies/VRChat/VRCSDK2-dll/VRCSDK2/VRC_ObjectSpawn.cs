using System.Collections.Generic;
using UnityEngine;

namespace VRCSDK2
{
	public class VRC_ObjectSpawn : MonoBehaviour, INetworkID, IVRCEventProvider
	{
		public delegate void InitializationDelegate(VRC_ObjectSpawn obj);

		public delegate void InstantiationDelegate(Vector3 position, Quaternion rotation);

		public delegate void ObjectReaperDelegate();

		public GameObject ObjectPrefab;

		public static InitializationDelegate Initialize;

		public InstantiationDelegate Instantiate;

		public ObjectReaperDelegate ReapObjects;

		[HideInInspector]
		public int networkId;

		[HideInInspector]
		public int NetworkID
		{
			get
			{
				return networkId;
			}
			set
			{
				networkId = value;
			}
		}

		public VRC_ObjectSpawn()
			: this()
		{
		}

		public void SpawnObject()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			SpawnObject(this.get_transform().get_position(), this.get_transform().get_rotation());
		}

		public void SpawnObject(Vector3 position, Quaternion rotation)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			if (Instantiate != null)
			{
				Instantiate(position, rotation);
			}
		}

		public void DestroySpawnedObjects()
		{
			if (ReapObjects != null)
			{
				ReapObjects();
			}
		}

		private void Start()
		{
			if (Initialize != null)
			{
				Initialize(this);
			}
		}

		public IEnumerable<VRC_EventHandler.VrcEvent> ProvideEvents()
		{
			List<VRC_EventHandler.VrcEvent> list = new List<VRC_EventHandler.VrcEvent>();
			VRC_EventHandler.VrcEvent vrcEvent = new VRC_EventHandler.VrcEvent();
			vrcEvent.Name = "SpawnObject";
			vrcEvent.EventType = VRC_EventHandler.VrcEventType.SendRPC;
			vrcEvent.ParameterString = "SpawnObject";
			vrcEvent.ParameterObjects = (GameObject[])new GameObject[1]
			{
				this.get_gameObject()
			};
			list.Add(vrcEvent);
			vrcEvent = new VRC_EventHandler.VrcEvent();
			vrcEvent.Name = "DestroySpawnedObjects";
			vrcEvent.EventType = VRC_EventHandler.VrcEventType.SendRPC;
			vrcEvent.ParameterString = "DestroySpawnedObjects";
			vrcEvent.ParameterObjects = (GameObject[])new GameObject[1]
			{
				this.get_gameObject()
			};
			list.Add(vrcEvent);
			return list;
		}
	}
}