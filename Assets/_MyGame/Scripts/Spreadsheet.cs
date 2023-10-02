using UnityEngine;

public class Spreadsheet : MonoBehaviour {
	public Vector2 columnRowHeaderSize = new Vector2(100, 40);
	public GameObject prefab_defaultCell;
	public Vector2 defaultCellSize = new Vector2(100, 30);
	public Column[] columns;
	public Row[] rows;
	public float[] rowsHeight;

	[System.Serializable]
	public class Column {
		public string label;
		public float width;
	}

	[System.Serializable]
	public class Row {
		public string label;
		public float height;
		public string[] output;
		public object data;
	}

	void Start() {

	}

	void Update() {

	}
}
