using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Utils;
using TransmittalCreator.ViewModel;

namespace TransmittalCreator.CadCommands
{
	public class CtTransmCommand
	{
		[CommandMethod("CtTransm")]
		public void ListAttributes()
		{
			Dictionary<string, string> attrList = new Dictionary<string, string>();

			MainWindow window = new MainWindow(new BlockViewModel(attrList));

			Application.ShowModalWindow(window);

			if (window.isClicked == true)
			{
				//var objectIds = Utils.GetAllCurrentSpaceBlocksByName(window.NameBlock.Text);
				ObjectId mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(Active.Database);
				ObjectIdCollection objectIds = PrintUtils.SelectDynamicBlockReferences(mSpaceId);

				List<Sheet> dict = new List<Sheet>();
				List<PrintModel> printModels = new List<PrintModel>();

				BlockAttribute objectNameEn = window.ComboObjectNameEn.SelectedItem as BlockAttribute;
				BlockAttribute objectNameRu = window.ComboObjectNameRu.SelectedItem as BlockAttribute;

				BlockAttribute position = window.ComboBoxPosition.SelectedItem as BlockAttribute;
				BlockAttribute nomination = window.ComboBoxNomination.SelectedItem as BlockAttribute;
				BlockAttribute comment = window.ComboBoxComment.SelectedItem as BlockAttribute;
				BlockAttribute trItem = window.ComboBoxTrItem.SelectedItem as BlockAttribute;
				BlockAttribute trDocNumber = window.ComboBoxTrDocNumber.SelectedItem as BlockAttribute;
				BlockAttribute trDocTitleEn = window.ComboBoxTrDocTitleEn.SelectedItem as BlockAttribute;
				BlockAttribute trDocTitleRu = window.ComboBoxTrDocTitleRu.SelectedItem as BlockAttribute;

				AttributModel attributModel = new AttributModel(objectNameEn, objectNameRu, position, nomination,
					comment, trItem, trDocNumber, trDocTitleEn, trDocTitleRu);

				using (Transaction tr = Active.Database.TransactionManager.StartTransaction())
				{
					PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, objectIds);
					string selAttrName = "НОМЕР_ЛИСТА";
					PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, objectIds, selAttrName);

					if (window.transmittalCheckBox.IsChecked == true)
					{

						PrintUtils.CreateOnlyVed(dict);
						PrintUtils.CreateOnlytrans(dict);
						foreach (var printModel in printModels)
						{
							PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
						}
					}
					else
					{
						//Utils utils = new Utils();
						//utils.CreateOnlyVed(dict);
						foreach (var printModel in printModels)
						{
							//PlotCurrentLayout(printModel.DocNumber, printModel.BlockExtents3d, printModel.StampViewName);
						}
					}

					tr.Commit();
				}
			}
		}
	}
}
