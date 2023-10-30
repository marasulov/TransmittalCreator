using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransmittalCreator.ViewModels.ViewModels;

namespace TransmittalCreator.Infrastructure
{
	public class FileNameFromPrintModel
	{

		private readonly string _attributeName;
		private readonly bool _isAttrChecked;
		private readonly bool _isNumberingChecked;
		private readonly bool _isXChecked;
		private readonly bool _isYChecked;
		private readonly string _prefix;
		private readonly string _sufix;
		public int NumberingValue { get; set; }

		public FileNameFromPrintModel(ICreatePdfVm createPdf)
		{
			_isNumberingChecked = createPdf.IsCheckedNumbering;
			_prefix = createPdf.Prefix;
			_sufix = createPdf.Suffix;
			_isAttrChecked = createPdf.IsAttributeChecked;
			_isXChecked = createPdf.IsX;
			_isYChecked = createPdf.IsY;
			_attributeName = createPdf?.SelectedAttr?.AttributeName;
			NumberingValue = createPdf.RenumberStartValue == null ? 0 : Int32.Parse(createPdf.RenumberStartValue);
		}

		
	}
}
