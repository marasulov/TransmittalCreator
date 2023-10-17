using System.Collections.Generic;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models;
using TransmittalCreator.Utils;

namespace TransmittalCreator.CadCommands;

public class CtTransmCommand
{
	//[CommandMethod("CtTransm")]
	public void ListAttributes()
	{
		var attrList = new Dictionary<string, string>();

		var window = new MainWindow( /*new BlockViewModel(attrList)*/);

		Application.ShowModalWindow(window);

		if (window.isClicked)
		{
			//var objectIds = Utils.GetAllCurrentSpaceBlocksByName(window.NameBlock.Text);
			var mSpaceId = SymbolUtilityServices.GetBlockModelSpaceId(Active.Database);
			var objectIds = PrintUtils.SelectDynamicBlockReferences(mSpaceId);

			var dict = new List<Sheet>();
			var printModels = new List<PrintModel>();

			var objectNameEn = window.ComboObjectNameEn.SelectedItem as BlockAttribute;
			var objectNameRu = window.ComboObjectNameRu.SelectedItem as BlockAttribute;

			var position = window.ComboBoxPosition.SelectedItem as BlockAttribute;
			var nomination = window.ComboBoxNomination.SelectedItem as BlockAttribute;
			var comment = window.ComboBoxComment.SelectedItem as BlockAttribute;
			var trItem = window.ComboBoxTrItem.SelectedItem as BlockAttribute;
			var trDocNumber = window.ComboBoxTrDocNumber.SelectedItem as BlockAttribute;
			var trDocTitleEn = window.ComboBoxTrDocTitleEn.SelectedItem as BlockAttribute;
			var trDocTitleRu = window.ComboBoxTrDocTitleRu.SelectedItem as BlockAttribute;

			var attributModel = new AttributModel(objectNameEn, objectNameRu, position, nomination,
				comment, trItem, trDocNumber, trDocTitleEn, trDocTitleRu);

			using (var tr = Active.Database.TransactionManager.StartTransaction())
			{
				PrintUtils.GetSheetsFromBlocks(Active.Editor, dict, tr, objectIds);
				var selAttrName = "НОМЕР_ЛИСТА";
				PrintUtils.GetPrintParametersToPdf(Active.Editor, printModels, tr, objectIds, selAttrName);

				if (window.transmittalCheckBox.IsChecked == true)
				{
					PrintUtils.CreateOnlyVed(dict);
					PrintUtils.CreateOnlytrans(dict);
					foreach (var printModel in printModels)
						PrintUtils.PlotCurrentLayout(printModel.DocNumber, printModel);
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