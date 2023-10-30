using Autodesk.AutoCAD.DatabaseServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TransmittalCreator.Models;

namespace TransmittalCreator.Services
{
	public interface IFileNameCreator
	{
		int NumberingValue { get; set; }
		//void GetPrintParametersToPdf(Transaction tr);
		string GetFileName(PrintModel printModel);
		List<PrintModel> GetPrintModels();
	}
}
