using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectangleMath : MonoBehaviour
{
	[Flags]
	public enum RectArea {
		None = 0,
		Top = 1,
		Bottom = 2,
		Vertical = 3,
		Left = 4,
		Right = 8,
		Horizontal = 12,
	}
}
