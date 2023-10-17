using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransmittalCreator.ViewModels.ViewModels
{
	public interface ICreatePdfVm
	{
		bool IsX { get; set; }
		bool IsY { get; set; }
		bool IsSelectedOrder { get; set; }
		List<BlockAttribute> Attributes { get; set; }
		BlockAttribute SelectedAttr { get; set; }
		string RenumberStartValue { get; set; }
		bool RenumberLayouts { get; set; }
		bool InsertTotalPages { get; set; }
		string Prefix { get; set; }
		string Suffix { get; set; }
		bool IsCheckedNumbering { get; set; }
		bool IsAttributeChecked { get; set; }
	}
}
