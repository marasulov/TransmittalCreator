using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using TransmittalCreator.Models;

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

        public List<PrintModel> PrintModels
        {
            get => _printModels;
            set => _printModels = value;
        }
        public int NumberingValue
        {
            get => _numberingValue;
            set => _numberingValue = value;
        }

        public FileNameCreator(Window1 window1)
        {
            _numberingValue = Int32.Parse(window1.numberingTextbox.Text);
            _prefix = window1.prefixTextBox.Text;
            _sufix = window1.suffixTextBox.Text;
            _isAttrChecked = window1.attributeCheckBox.IsChecked.Value;
            _isXChecked= window1.RadioButtonX.IsChecked.Value;
            _isXChecked= window1.RadioButtonY.IsChecked.Value;

        }

        public FileNameCreator(Window1 window1, ObjectId[] ids)
        {
            _isNumberingChecked = window1.numberingCheckbox.IsChecked.Value;
            _prefix = window1.prefixTextBox?.Text;
            _sufix = window1.suffixTextBox?.Text;
            _isAttrChecked = window1.attributeCheckBox.IsChecked.Value;
            _isXChecked= window1.RadioButtonX.IsChecked.Value;
            _isYChecked= window1.RadioButtonY.IsChecked.Value;
            if(_isAttrChecked && !string.IsNullOrEmpty(_attributeName))
            {
                BlockAttribute blockAttribute = window1.comboAttributs.SelectedItem as BlockAttribute;
                _attributeName = blockAttribute.AttributeName;
            }

            if (_isAttrChecked) _numberingValue = Int32.Parse(window1.numberingTextbox.Text);

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
                _printModels.Add(new PrintModel(docNumber,blkId));
                btr.Dispose();
            }

        }



        private string GetFileName(PrintModel printModel)
        {
            string attributeValue = printModel.DocNumber;
            if(_isNumberingChecked)
                _numberingValue = NumberingValue + 1 ;
            return _prefix + attributeValue + NumberingValue + _sufix+".pdf";
        }

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
            //TODO correct attrname
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

        private void SortPrintModelsBySelectedOrder()
        {
            if(_isXChecked) _printModels = _printModels.OrderBy(x => x.MinPointX).ToList();
            else if (_isYChecked)
            {
                _printModels = _printModels.OrderByDescending(x => x.MinPointY).ToList();
            }
        }

        public List<PrintModel> GetPrintModels()
        {
            SortPrintModelsBySelectedOrder();

            foreach (var printModel in PrintModels)
            {
                printModel.DocNumber = GetFileName(printModel);
            }

            return PrintModels;
        }
    }
}
