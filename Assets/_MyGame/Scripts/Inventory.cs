using System.Collections;
using UnityEngine;

namespace MyGame {
	public class Inventory : MonoBehaviour {
		[ContextMenuItem(nameof(DropFirstItem),nameof(DropFirstItem))]
		public Collider itemGrabber;

		private void Start() {
			InventoryItemGrabber invGrabber = itemGrabber.gameObject.AddComponent<InventoryItemGrabber>();
			invGrabber.inventory = this;
		}

		public void Acquire(Item item) {
			if (!item.enabled) {
				return;
			}
			item.transform.SetParent(transform);
			item.gameObject.SetActive(false);
			item.onAcquired.Invoke();
		}

		public Item GetItemNamed(string name) {
			Transform myTransform = transform;
			for (int i = 0; i < myTransform.childCount; ++i) {
				Transform child = myTransform.GetChild(i);
				if (child.name == name) {
					Item item = child.GetComponent<Item>();
					if (item == null || !item.enabled) {
						continue;
					}
					return item;
				}
			}
			return null;
		}

		public void DropFirstItem() {
			DropItem(0);
		}

		public void DropItem(Item item) {
			SetItemUngrabableTime(item, 3);
			item.transform.SetParent(null);
			item.gameObject.SetActive(true);
		}

		public void DropItem(int index) {
			if (index >= 0 && index < transform.childCount) {
				DropItem(transform.GetChild(index).GetComponent<Item>());
			}
		}

		public void DropItemNamed(string name) {
			Item item = GetItemNamed(name);
			if (item == null) {
				return;
			}
			DropItem(item);
		}

		public void SetItemUngrabableTime(Item item, float duration) {
			item.enabled = false;
			StartCoroutine(MakeUngrabableTimerCoroutine());
			IEnumerator MakeUngrabableTimerCoroutine() {
				yield return new WaitForSeconds(duration);
				item.enabled = true;
			}
		}

		public class InventoryItemGrabber : MonoBehaviour {
			[ContextMenuItem(nameof(DropFirstItem), nameof(DropFirstItem))]
			public Inventory inventory;
			private void OnCollisionEnter(Collision collision) {
				Item item = collision.gameObject.GetComponent<Item>();
				if (item != null) {
					inventory.Acquire(item);
				}
			}

			private void OnTriggerEnter(Collider other) {
				Item item = other.gameObject.GetComponent<Item>();
				if (item != null) {
					inventory.Acquire(item);
				}
			}
			public void DropFirtItem() => inventory.DropFirstItem();
		}
	}
}
