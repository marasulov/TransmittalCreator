using System.Collections.Generic;

namespace TransmittalCreator.Services.CheckListCreator;

public class CheckListModel
{
	public string CheckListFileName { get; set; }
	public List<Sheet> DictSheets { get; set; }
}