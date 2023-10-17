namespace TransmittalCreator.Models;

public class AttributModel
{
	public AttributModel(string position, string nomination, string comment, string trItem, string trDocNumber,
		string trDocTitleEn, string trDocTitleRu)
	{
		Position = position;
		Nomination = nomination;
		Comment = comment;
		TrItem = trItem;
		TrDocNumber = trDocNumber;
		TrDocTitleEn = trDocTitleEn;
		TrDocTitleRu = trDocTitleRu;
	}

	public AttributModel(BlockAttribute objectNameEn, BlockAttribute objectNameRu, BlockAttribute position,
		BlockAttribute nomination, BlockAttribute comment, BlockAttribute trItem, BlockAttribute trDocNumber,
		BlockAttribute trDocTitleEn, BlockAttribute trDocTitleRu)
	{
		ObjectNameEn = objectNameEn.AttributeName;
		ObjectNameRu = objectNameRu.AttributeName;
		Position = position.AttributeName;
		Nomination = nomination.AttributeName;
		if (comment != null)
			Comment = comment.AttributeName;
		TrItem = trItem.AttributeName;
		TrDocNumber = trDocNumber.AttributeName;
		TrDocTitleEn = trDocTitleEn.AttributeName;
		TrDocTitleRu = trDocTitleRu.AttributeName;
	}


	/// <summary>
	///     Номер листа
	/// </summary>
	public string ObjectNameEn { get; set; }

	public string ObjectNameRu { get; set; }

	/// <summary>
	///     Номер листа
	/// </summary>
	public string Position { get; set; }

	/// <summary>
	///     Document Number
	/// </summary>
	public string Nomination { get; set; }

	/// <summary>
	///     Document Name in English
	/// </summary>
	public string Comment { get; set; }

	/// <summary>
	///     Document Title (English)
	/// </summary>
	public string TrItem { get; set; }

	/// <summary>
	///     documnet name in rus
	/// </summary>
	public string TrDocNumber { get; set; }

	/// <summary>
	///     Document Title (Rus)
	/// </summary>
	public string TrDocTitleEn { get; set; }

	public string TrDocTitleRu { get; set; }
}