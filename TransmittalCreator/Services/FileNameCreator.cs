using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using TransmittalCreator.Models;
using TransmittalCreator.ViewModels.ViewModels;

namespace TransmittalCreator.Services;

public class FileNameCreator
{
	private readonly string _attributeName;
	private readonly bool _isAttrChecked;
	private readonly bool _isNumberingChecked;
	private readonly bool _isXChecked;
	private readonly bool _isYChecked;
	private readonly string _prefix;
	private readonly string _sufix;
	private readonly ObjectIdCollection _objectIdCollection;

	private List<PrintModel> _printModels = new();

	public FileNameCreator(ICreatePdfVm createPdf, ObjectIdCollection ids)
	{
		_isNumberingChecked = createPdf.IsCheckedNumbering;
		_prefix = createPdf.Prefix;
		_sufix = createPdf.Suffix;
		_isAttrChecked = createPdf.IsAttributeChecked;
		_isXChecked = createPdf.IsX;
		_isYChecked = createPdf.IsY;
		_attributeName = createPdf?.SelectedAttr?.AttributeName;
		NumberingValue = Int32.Parse(createPdf.RenumberStartValue);

		_objectIdCollection = ids;
	}

	//public List<PrintModel> PrintModels
	//{
	//    get => _printModels;
	//    set => _printModels = value;
	//}
	public int NumberingValue { get; set; }

	public void GetPrintParametersToPdf(Transaction tr)
	{
		foreach (ObjectId blkId in _objectIdCollection)
		{
			var blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForRead);
			var btr = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);

			var docNumber = GetBlockAttributeValue(blkRef);
			//formatValue = attrDict.FirstOrDefault(x => x.Key == "ФОРМАТ").Value;
			//ed.WriteMessage("\nBlock:{0} - {1} габариты {2} -{3}", btr.Name, docNumber, posPoint2d.ToString(), formatValue);
			_printModels.Add(new PrintModel(docNumber, blkId));
			btr.Dispose();
		}
	}

	private string GetFileName(PrintModel printModel)
	{
		var filename = _prefix + printModel.DocNumber + NumberingValue + _sufix;
		if (_isNumberingChecked) NumberingValue++;

		return filename;

	}

	//TOdo delete if success on blockutils
	private string GetBlockAttributeValue(BlockReference blkRef)
	{
		var docNumber = "";
		var props = blkRef.DynamicBlockReferencePropertyCollection;
		var blockStamp = "";
		foreach (DynamicBlockReferenceProperty prop in props)
			if (prop.PropertyName == "Штамп")
				blockStamp = prop.Value.ToString();

		var attrDict = blkRef.GetAttributesValues();
		if (blockStamp == "Форма 3 ГОСТ Р 21.1101-2009 M25" || blockStamp == "Форма 3 ГОСТ Р 21.1101-2009")
		{
			docNumber = attrDict.FirstOrDefault(x => x.Key == _attributeName).Value;
		}
		else if (blockStamp == "Форма 6 ГОСТ Р 21.1101-2009")
		{
			docNumber = attrDict.FirstOrDefault(x => x.Key == _attributeName).Value;
			docNumber += "-" + attrDict.FirstOrDefault(x => x.Key == "ЛИСТ2_СПЕЦ").Value;
		}

		return docNumber;
	}

	private void SortPrintModelsBySelectedValue()
	{
		if (_isXChecked) _printModels = _printModels.OrderBy(x => x.MinPointX).ToList();
		if (_isYChecked) _printModels = _printModels.OrderByDescending(x => x.MinPointY).ToList();
	}

	//TOdo delete if success on blockutils
	public List<PrintModel> GetPrintModels()
	{
		SortPrintModelsBySelectedValue();

		foreach (var printModel in _printModels) printModel.DocNumber = GetFileName(printModel);

		return _printModels;
	}
}