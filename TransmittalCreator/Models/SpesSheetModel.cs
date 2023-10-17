using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TransmittalCreator.Models;

public class SpesSheetModel : INotifyPropertyChanged
{
	private string docNumber;
	private string objectNameEng;

	private string sheetNumber;

	public SpesSheetModel(string attr, string val)
	{
		SheetNumber = attr;
		DocNumber = val;
	}

	/// <summary>
	///     Номер листа
	/// </summary>
	public string SheetNumber
	{
		get => sheetNumber;
		set
		{
			sheetNumber = value;
			OnPropertyChanged();
		}
	}

	/// <summary>
	///     Document Number
	/// </summary>
	public string DocNumber
	{
		get => docNumber;
		set
		{
			docNumber = value;
			OnPropertyChanged();
		}
	}

	/// <summary>
	///     Document Name in English
	/// </summary>
	public string ObjectNameEng { get; set; }

	/// <summary>
	///     Document Title (English)
	/// </summary>
	public string DocTitleEng { get; set; }

	/// <summary>
	///     documnet name in rus
	/// </summary>
	public string ObjectNameRu { get; set; }

	/// <summary>
	///     Document Title (Rus)
	/// </summary>
	public string DocTitleRu { get; set; }


	public event PropertyChangedEventHandler PropertyChanged;

	public void OnPropertyChanged([CallerMemberName] string prop = "")
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
	}
}