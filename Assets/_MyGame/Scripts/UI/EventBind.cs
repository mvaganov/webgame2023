using UnityEngine.Events;
using UnityEngine.EventSystems;

public class EventBind {
	public static EventTrigger.Entry GetEventEntry(EventTrigger eventTrigger, EventTriggerType type) {
		for (int i = 0; i < eventTrigger.triggers.Count; ++i) {
			if (eventTrigger.triggers[i].eventID == type) {
				return eventTrigger.triggers[i];
			}
		}
		return null;
	}

	public static void SetEvent(EventTrigger eventTrigger, EventTriggerType type, UnityAction<BaseEventData> action) {
		EventTrigger.Entry entry = GetEventEntry(eventTrigger, type);
		if (entry == null) {
			entry = new EventTrigger.Entry() {
				eventID = type,
			};
			eventTrigger.triggers.Add(entry);
		}
		entry.callback.RemoveListener(action);
		entry.callback.AddListener(action);
	}
}
