using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RectangleMath
{
	[Flags]
	public enum RectDirection {
		None = 0,      // 0000
		MinY = 1,      // 0001
		MaxY = 2,      // 0010
		AllY = 3,      // 0011
		MinX = 4,      // 0100
		MinXMinY = 5,  // 0101
		MinXMaxY = 6,  // 0110
		NotMaxX = 7,   // 0111
		MaxX = 8,      // 1000
		MaxXMinY = 9,  // 1001
		MaxXMaxY = 10, // 1010
		NotMinX = 11,  // 1011
		AllX = 12,     // 1100
		NotMaxY = 13,  // 1101
		NotMinY = 14,  // 1110
		All = 15,      // 1111
	}

	public static RectDirection CalculateOverlap(this Rect self, Rect target) {
		RectDirection result = RectDirection.None;
		Vector2 selfMin = self.min, selfMax = self.max, rectMin = target.min, rectMax = target.max;
		bool hasMinX = rectMin.x >= selfMin.x && rectMin.x <= selfMax.x;
		bool hasMaxX = rectMax.x >= selfMin.x && rectMax.x <= selfMax.x;
		bool hasMinY = rectMin.y >= selfMin.y && rectMin.y <= selfMax.y;
		bool hasMaxY = rectMax.y >= selfMin.y && rectMax.y <= selfMax.y;
		if (hasMinX) { result |= RectDirection.MinX; }
		if (hasMaxX) { result |= RectDirection.MaxX; }
		if (hasMinY) { result |= RectDirection.MinY; }
		if (hasMaxY) { result |= RectDirection.MaxY; }
		return result;
	}

	public static bool TryCalcDeltaToInclude(this Rect self, Rect target, out Vector2 delta) {
		delta = Vector2.zero;
		Vector2 selfMin = self.min, selfMax = self.max, rectMin = target.min, rectMax = target.max;
		Vector2 selfSize = self.size, rectSize = target.size;
		Vector2 targetPoint = self.center;
		bool fitsX = selfSize.x >= rectSize.x;
		bool fitsY = selfSize.y >= rectSize.y;
		bool hasMinX = rectMin.x >= selfMin.x && rectMin.x <= selfMax.x;
		bool hasMaxX = rectMax.x >= selfMin.x && rectMax.x <= selfMax.x;
		bool hasMinY = rectMin.y >= selfMin.y && rectMin.y <= selfMax.y;
		bool hasMaxY = rectMax.y >= selfMin.y && rectMax.y <= selfMax.y;
		bool change = TryGetTarget('x',selfMin.x, selfMax.x, fitsX, hasMaxX, hasMinX, rectMin.x, rectMax.x, ref targetPoint.x)
		| TryGetTarget('y',selfMin.y, selfMax.y, fitsY, hasMaxY, hasMinY, rectMin.y, rectMax.y, ref targetPoint.y);
		if (!change) {
			return false;
		}
		change |= self.RequiredToInclude(targetPoint, out delta);
		//Debug.Log($"target point: {targetPoint}, needs {delta}");
		return change;
	}

	private static bool TryGetTarget(char dim, float selfMin_, float selfMax_, bool fits, bool hasMax, bool hasMin, float rectMin_, float rectMax_, ref float targetPoint_) {
		if (fits) {
			if (hasMax && !hasMin) {
				//Debug.Log($"{dim} need to see min");
				targetPoint_ = rectMin_;
			} else if (hasMin && !hasMax) {
				//Debug.Log($"{dim} need to see max");
				targetPoint_ = rectMax_;
			} else {
				float distMin = Mathf.Abs(targetPoint_ - rectMin_);
				float distMax = Mathf.Abs(targetPoint_ - rectMax_);
				if (distMin > distMax) {
					//Debug.Log($"{dim} too far, need to see min");
					targetPoint_ = rectMin_;
				} else {
					//Debug.Log($"{dim} too far, need to see max");
					targetPoint_ = rectMax_;
				}
			}
			return true;
		} else {
			bool alreadyLooking = rectMin_ <= selfMin_ && rectMax_ >= selfMax_;
			if (!alreadyLooking && !hasMax && !hasMin) {
				float distMin = Mathf.Abs(targetPoint_ - rectMin_);
				float distMax = Mathf.Abs(targetPoint_ - rectMax_);
				float selfSize = selfMax_ - selfMin_;
				if (distMin < distMax) {
					//Debug.Log($"{dim} too wide, need to see min");
					targetPoint_ = rectMin_ + selfSize;
				} else {
					//Debug.Log($"{dim} too wide, need to see max");
					targetPoint_ = rectMax_ - selfSize;
				}
				return true;
			}
			//Debug.Log($"{dim} already saturated");
		}
		return false;
	}

	public static bool RequiredToInclude(this Rect self, Vector2 target, out Vector2 delta) {
		delta = Vector2.zero;
		Vector2 selfMin = self.min, selfMax = self.max;
		bool changeRequired = false;
		if (target.x < selfMin.x) { delta.x = target.x - selfMin.x; changeRequired = true; }
		if (target.x > selfMax.x) { delta.x = target.x - selfMax.x; changeRequired = true; }
		if (target.y < selfMin.y) { delta.y = target.y - selfMin.y; changeRequired = true; }
		if (target.y > selfMax.y) { delta.y = target.y - selfMax.y; changeRequired = true; }
		return changeRequired;
	}
}
