using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spreadsheet {
	public partial class Spreadsheet {
		[ContextMenuItem(nameof(InitializeColumnHeaders), nameof(InitializeColumnHeaders))]
		public RectTransform ColumnHeadersArea;
		[ContextMenuItem(nameof(InitializeRowHeaders), nameof(InitializeRowHeaders))]
		public RectTransform RowHeadersArea;

		public void AdjustColumnHeaders(Vector2 scroll) {
			ColumnHeadersArea.anchoredPosition = new Vector2(ContentArea.anchoredPosition.x, ColumnHeadersArea.anchoredPosition.y);
		}

		public void AdjustRowHeaders(Vector2 scroll) {
			RowHeadersArea.anchoredPosition = new Vector2(RowHeadersArea.anchoredPosition.x, ContentArea.anchoredPosition.y);
		}
	}
}
