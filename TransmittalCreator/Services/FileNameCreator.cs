using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using TransmittalCreator.Models;
using TransmittalCreator.ViewModels.ViewModels;

namespace TransmittalCreator.Services
{
	public class FileNameCreator
	{
		private readonly string _attributeName;
		private readonly bool _isNumberingChecked;
		private int _numberingValue;
		private readonly string _prefix;
		private readonly string _sufix;
		private readonly bool _isAttrChecked;
		private readonly bool _isXChecked;
		private readonly bool _isYChecked;

		private List<PrintModel> _printModels = new List<PrintModel>();
		private ObjectIdCollection _objectIdCollection;

		//public List<PrintModel> PrintModels
		//{
		//    get => _printModels;
		//    set => _printModels = value;
		//}
		public int NumberingValue
		{
			get => _numberingValue;
			set => _numberingValue = value;
		}

		public FileNameCreator(ICreatePdfVm createPdf, ObjectId[] ids)
		{
			_isNumberingChecked = createPdf.IsCheckedNumbering;
			_prefix = createPdf.Prefix;
			_sufix = createPdf.Suffix;
			_isAttrChecked = createPdf.IsAttributeChecked;
			_isXChecked = createPdf.IsX;
			_isYChecked = createPdf.IsY;
			_attributeName = createPdf.SelectedAttr.AttributeName;
			_numberingValue = createPdf.RenumberStartValue;

			_objectIdCollection = new ObjectIdCollection(ids);
		}

		public void GetPrintParametersToPdf(Transaction tr)
		{
			foreach (ObjectId blkId in _objectIdCollection)
			{
				BlockReference blkRef = (BlockReference)tr.GetObject(blkId, OpenMode.ForRead);
				BlockTableRecord btr = (BlockTableRecord)tr.GetObject(blkRef.BlockTableRecord, OpenMode.ForRead);

				string docNumber = GetBlockAttributeValue(blkRef);
				//formatValue = attrDict.FirstOrDefault(x => x.Key == "ФОРМАТ").Value;
				//ed.WriteMessage("\nBlock:{0} - {1} габариты {2} -{3}", btr.Name, docNumber, posPoint2d.ToString(), formatValue);
				_printModels.Add(new PrintModel(docNumber, blkId));
				btr.Dispose();
			}

		}

		private string GetFileName(PrintModel printModel)
		{
			if (_isNumberingChecked)
			{
				_numberingValue = NumberingValue + 1;
			}
			return _prefix + printModel.DocNumber + NumberingValue + _sufix;
		}

		//TOdo delete if success on blockutils
		private string GetBlockAttributeValue(BlockReference blkRef)
		{
			string docNumber = "";
			DynamicBlockReferencePropertyCollection props = blkRef.DynamicBlockReferencePropertyCollection;
			string blockStamp = "";
			foreach (DynamicBlockReferenceProperty prop in props)
			{
				if (prop.PropertyName == "Штамп")
				{
					blockStamp = prop.Value.ToString();
				}
			}

			var attrDict = AttributeExtensions.GetAttributesValues(blkRef);
			if (blockStamp == "Форма 3 ГОСТ Р 21.1101-2009 M25" || blockStamp == "Форма 3 ГОСТ Р 21.1101-2009")
				docNumber = attrDict.FirstOrDefault(x => x.Key == _attributeName).Value;
			else if (blockStamp == "Форма 6 ГОСТ Р 21.1101-2009")
			{
				docNumber = attrDict.FirstOrDefault(x => x.Key == _attributeName).Value;
				docNumber += "-" + attrDict.FirstOrDefault(x => x.Key == "ЛИСТ2_СПЕЦ").Value;
			}
			return docNumber;
		}

		private void SortPrintModelsBySelectedValue()
		{
			if (_isXChecked)
			{
				_printModels = _printModels.OrderBy(x => x.MinPointX).ToList();
			}
			if (_isYChecked)
			{
				_printModels = _printModels.OrderByDescending(x => x.MinPointY).ToList();
			}
		}

		//TOdo delete if success on blockutils
		public List<PrintModel> GetPrintModels()
		{
			SortPrintModelsBySelectedValue();

			foreach (var printModel in _printModels)
			{
				printModel.DocNumber = GetFileName(printModel);
			}

			return _printModels;
		}
	}
}
