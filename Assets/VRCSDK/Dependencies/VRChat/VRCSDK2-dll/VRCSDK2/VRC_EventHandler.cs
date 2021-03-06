using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRCSDK2
{
	[ExecuteInEditMode]
	public class VRC_EventHandler : VRC_Behaviour, INetworkID
	{
		public enum VrcEventType
		{
			MeshVisibility = 0,
			AnimationFloat = 1,
			AnimationBool = 2,
			AnimationTrigger = 3,
			AudioTrigger = 4,
			PlayAnimation = 5,
			SendMessage = 6,
			SetParticlePlaying = 7,
			TeleportPlayer = 8,
			RunConsoleCommand = 9,
			SetGameObjectActive = 10,
			SetWebPanelURI = 11,
			SetWebPanelVolume = 12,
			SpawnObject = 13,
			SendRPC = 14,
			ActivateCustomTrigger = 0xF,
			DestroyObject = 0x10,
			SetLayer = 17,
			SetMaterial = 18,
			AddHealth = 19,
			AddDamage = 20,
			SetComponentActive = 21,
			AnimationInt = 22,
			AnimationIntAdd = 24,
			AnimationIntSubtract = 25,
			AnimationIntMultiply = 26,
			AnimationIntDivide = 27,
			AddVelocity = 28,
			SetVelocity = 29,
			AddAngularVelocity = 30,
			SetAngularVelocity = 0x1F,
			AddForce = 0x20,
			SetUIText = 33
		}

		public enum VrcBroadcastType
		{
			Always,
			Master,
			Local,
			Owner,
			AlwaysUnbuffered,
			MasterUnbuffered,
			OwnerUnbuffered,
			AlwaysBufferOne,
			MasterBufferOne,
			OwnerBufferOne
		}

		public enum VrcTargetType
		{
			All,
			Others,
			Owner,
			Master,
			AllBuffered,
			OthersBuffered,
			Local,
			AllBufferOne,
			OthersBufferOne,
			TargetPlayer
		}

		public enum VrcBooleanOp
		{
			Unused = -1,
			False,
			True,
			Toggle
		}

		[Serializable]
		public class VrcEvent
		{
			public string Name = string.Empty;

			public VrcEventType EventType = VrcEventType.AudioTrigger;

			public string ParameterString = string.Empty;

			public VrcBooleanOp ParameterBoolOp = VrcBooleanOp.Unused;

			[HideInInspector]
			public bool ParameterBool;

			public float ParameterFloat;

			public int ParameterInt = 1;

			[HideInInspector]
			public GameObject ParameterObject;

			[SerializeField]
			public GameObject[] ParameterObjects;

			[HideInInspector]
			[SerializeField]
			public byte[] ParameterBytes;

			[SerializeField]
			[HideInInspector]
			public int? ParameterBytesVersion;
		}

		public class EventInfo
		{
			public VrcEvent evt;

			public VrcBroadcastType broadcast;

			public int instagatorId;

			public float fastForward;
		}

		public delegate long AssignNetworkIdDelegate(VRC_EventHandler obj);

		public delegate int GetNetworkIdDelegate(GameObject obj);

		public delegate void LogEventDelegate(VRC_EventHandler eventHandler, VrcEvent vrcEvent, long combinedNetworkId, VrcBroadcastType broadcast, int instagatorId, float fastForward);

		[SerializeField]
		public List<VrcEvent> Events = new List<VrcEvent>();

		public long CombinedNetworkId = -1L;

		public VRC_EventDispatcher _dispatcher;

		public bool _registered;

		public static AssignNetworkIdDelegate AssignCombinedNetworkId;

		public static GetNetworkIdDelegate GetInstigatorId;

		public static LogEventDelegate LogEvent;

		private bool _readyForEvents;

		public List<EventInfo> deferredEvents = new List<EventInfo>();

		public int NetworkID
		{
			get;
			set;
		}

		public static GetNetworkIdDelegate GetInsitgatorId
		{
			get
			{
				return GetInstigatorId;
			}
			set
			{
				GetInstigatorId = value;
			}
		}

		public static bool BooleanOp(VrcBooleanOp Op, bool Current)
		{
			switch (Op)
			{
			case VrcBooleanOp.False:
				return false;
			case VrcBooleanOp.True:
				return true;
			case VrcBooleanOp.Toggle:
				return !Current;
			case VrcBooleanOp.Unused:
				return Current;
			default:
				return false;
			}
		}

		private void Awake()
		{
			VRC_EventHandler[] components = this.get_gameObject().GetComponents<VRC_EventHandler>();
			if (components.Length > 1 || components[0] != this)
			{
				Debug.LogError((object)("Multiple event handlers found on " + this.get_gameObject().get_name() + ", bad things will happen."));
			}
			foreach (VrcEvent @event in Events)
			{
				if (@event.ParameterBoolOp == VrcBooleanOp.Unused)
				{
					if (@event.ParameterBool)
					{
						@event.ParameterBoolOp = VrcBooleanOp.True;
					}
					else
					{
						@event.ParameterBoolOp = VrcBooleanOp.False;
					}
				}
			}
		}

		private void Start()
		{
			if (Application.get_isPlaying())
			{
				if (AssignCombinedNetworkId != null)
				{
					CombinedNetworkId = AssignCombinedNetworkId(this);
				}
				if (_dispatcher == null)
				{
					_dispatcher = this.get_gameObject().GetComponent<VRC_EventDispatcherLocal>();
					if (_dispatcher == null)
					{
						_dispatcher = this.get_gameObject().AddComponent<VRC_EventDispatcherLocal>();
					}
				}
			}
		}

		public void VrcAnimationEvent(AnimationEvent aEvent)
		{
			foreach (VrcEvent @event in Events)
			{
				if (!(@event.Name != aEvent.get_stringParameter()) && !(_dispatcher == null))
				{
					TriggerEvent(@event, VrcBroadcastType.Local, 0, 0f);
				}
			}
		}

		public void TriggerEvent(VrcEvent e, VrcBroadcastType broadcast, GameObject instagator = null, float fastForward = 0f)
		{
			int instagatorId = 0;
			if (GetInstigatorId != null && instagator != null)
			{
				instagatorId = GetInstigatorId(instagator);
			}
			TriggerEvent(e, broadcast, instagatorId, fastForward);
		}

		public void TriggerEvent(VrcEvent e, VrcBroadcastType broadcast, int instagatorId, float fastForward)
		{
			if (e != null && !(this.get_gameObject() == null))
			{
				if (_dispatcher == null)
				{
					Debug.LogFormat("Deferring event {0} of type {1} because dispatcher is unavailable.", new object[2]
					{
						e.Name,
						e.EventType
					});
					DeferEvent(e, broadcast, instagatorId, fastForward);
				}
				else if (!Networking.IsNetworkSettled)
				{
					Debug.LogFormat("Deferring event {0} of type {1} because the network is not settled.", new object[2]
					{
						e.Name,
						e.EventType
					});
					DeferEvent(e, broadcast, instagatorId, fastForward);
				}
				else if ((e.ParameterObjects == null || e.ParameterObjects.Length == 0) && e.ParameterObject == null)
				{
					Debug.LogError((object)("No object to receive event " + e.Name + " of type " + e.EventType));
				}
				else
				{
					if (e.ParameterObjects != null)
					{
						GameObject[] parameterObjects = e.ParameterObjects;
						foreach (GameObject parameterObject in parameterObjects)
						{
							GameObject parameterObject2 = e.ParameterObject;
							e.ParameterObject = parameterObject;
							InternalTriggerEvent(e, broadcast, instagatorId, fastForward);
							e.ParameterObject = parameterObject2;
						}
					}
					if (e.ParameterObject != null)
					{
						InternalTriggerEvent(e, broadcast, instagatorId, fastForward);
					}
				}
			}
		}

		private void InternalTriggerEvent(VrcEvent e, VrcBroadcastType broadcast, int instagatorId, float fastForward)
		{
			if (LogEvent != null)
			{
				LogEvent(this, e, CombinedNetworkId, broadcast, instagatorId, fastForward);
			}
			else
			{
				_dispatcher.TriggerEvent(this, e, broadcast, instagatorId, fastForward);
			}
		}

		[Obsolete("Do not trigger events by name", false)]
		public void TriggerEvent(string eventName, VrcBroadcastType broadcast, GameObject instagator = null, int instagatorId = 0)
		{
			if (instagator != null && instagatorId <= 0 && GetInstigatorId != null)
			{
				instagatorId = GetInstigatorId(instagator);
			}
			foreach (VrcEvent @event in Events)
			{
				if (!(@event.Name != eventName) && !(_dispatcher == null))
				{
					TriggerEvent(@event, broadcast, instagatorId, 0f);
				}
			}
		}

		[Obsolete("Do not trigger events by name", false)]
		public void TriggerEvent(string eventName, VrcBroadcastType broadcast, GameObject instagator, int instagatorId, float fastForward)
		{
			if (instagator != null && instagatorId <= 0 && GetInstigatorId != null)
			{
				instagatorId = GetInstigatorId(instagator);
			}
			foreach (VrcEvent @event in Events)
			{
				if (!(@event.Name != eventName) && !(_dispatcher == null))
				{
					TriggerEvent(@event, broadcast, instagatorId, fastForward);
				}
			}
		}

		private void OnValidate()
		{
			foreach (VrcEvent @event in Events)
			{
				if (@event.ParameterBoolOp == VrcBooleanOp.Unused)
				{
					if (@event.ParameterBool)
					{
						@event.ParameterBoolOp = VrcBooleanOp.True;
					}
					else
					{
						@event.ParameterBoolOp = VrcBooleanOp.False;
					}
				}
			}
		}

		public void Unregister()
		{
			if (_dispatcher != null && _registered)
			{
				_dispatcher.UnregisterEventHandler(this);
			}
			_registered = false;
		}

		private void OnDestroy()
		{
			ProcessDeferredEvents();
			if (deferredEvents.Count > 0)
			{
				Debug.LogError((object)"Not all events were triggered prior to the handler being destroyed.");
			}
			Unregister();
		}

		private void LateUpdate()
		{
			ProcessDeferredEvents();
		}

		public long GetCombinedNetworkId()
		{
			if (CombinedNetworkId <= 0)
			{
				Debug.LogError((object)"Combined Network IDs must be initialized by now");
			}
			return CombinedNetworkId;
		}

		public static bool HasEventTrigger(GameObject go)
		{
			return go.GetComponent<VRC_Interactable>() != null;
		}

		public void SetReady(bool ready)
		{
			_readyForEvents = ready;
		}

		public bool IsReadyForEvents()
		{
			return _readyForEvents;
		}

		public void DeferEvent(VrcEvent e, VrcBroadcastType broadcast, int instagatorId, float fastForward)
		{
			if (deferredEvents == null)
			{
				deferredEvents = new List<EventInfo>();
			}
			deferredEvents.Add(new EventInfo
			{
				evt = e,
				broadcast = broadcast,
				instagatorId = instagatorId,
				fastForward = fastForward
			});
		}

		private void ProcessDeferredEvents()
		{
			if (Networking.IsNetworkSettled && _dispatcher != null && deferredEvents != null && deferredEvents.Count > 0 && Networking.IsObjectReady(this.get_gameObject()))
			{
				List<EventInfo> list = new List<EventInfo>(deferredEvents);
				deferredEvents = new List<EventInfo>();
				foreach (EventInfo item in list)
				{
					if (item != null)
					{
						TriggerEvent(item.evt, item.broadcast, item.instagatorId, item.fastForward);
					}
				}
			}
		}
	}
}
