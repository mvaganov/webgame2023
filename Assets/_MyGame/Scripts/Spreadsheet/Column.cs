namespace Spreadsheet {
	[System.Serializable]
	public class Column {
		public string label;
		public float width;
		public int cellType;
		private System.Func<object, object> getData;
		private System.Func<object, object, Parse.Error> setData;
		public System.Func<object, object> GetData { get => getData; set => getData = value; }
		public System.Func<object, object, Parse.Error> SetData { get => setData; set => setData = value; }
	}
}
