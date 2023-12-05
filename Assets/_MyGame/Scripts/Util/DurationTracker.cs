using System;
using System.Collections.Generic;

namespace Spreadsheet {
	public class DurationTracker<T> {
		private Dictionary<T, float> _ledger = new Dictionary<T, float>();
		public Dictionary<T, float> Ledger => _ledger;
		public void AddDuration(T key, float additionalValue) {
			if (TryGetDuration(key, out float currentValue)) {
				SetDuration(key, currentValue + additionalValue);
			} else {
				SetDuration(key, additionalValue);
			}
		}

		public void SetDuration(T key, float value) {
			_ledger[key] = value;
		}

		public bool TryGetDuration(T key, out float value) {
			return _ledger.TryGetValue(key, out value);
		}

		public void ClearDuration(T key) {
			_ledger.Remove(key);
		}
	}
}
