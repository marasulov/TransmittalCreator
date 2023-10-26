using System;
using System.Collections.Generic;
using System.Windows;
using System.Xml.Linq;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using TransmittalCreator.Utils;
using Application = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using Exception = System.Exception;

namespace TransmittalCreator.DBCad;

/// <summary>
///     Provides easy access to several "active" objects in the AutoCAD
///     runtime environment.
/// </summary>
public static class Active
{
	public delegate void TransactionDelegate(Transaction tr);

	/// <summary>
	///     Returns the active Editor object.
	/// </summary>
	public static Editor Editor => Document.Editor;

	/// <summary>
	///     Returns the active Document object.
	/// </summary>
	public static Document Document => Application.DocumentManager.MdiActiveDocument;

	/// <summary>
	///     Returns the active Database object.
	/// </summary>
	public static Database Database => Document.Database;

	/// <summary>
	///     Список слоев текущей базы данных
	/// </summary>
	public static List<string> Layers
	{
		get
		{
			var layers = new List<string>();
			using (Document.LockDocument())
			{
				using (var tr = Database.TransactionManager.StartOpenCloseTransaction())
				{
					if (Database.LayerTableId.IsValid &&
					    tr.GetObject(Database.LayerTableId, OpenMode.ForRead) is LayerTable lt)
						foreach (var layerId in lt)
							try
							{
								if (tr.GetObject(layerId, OpenMode.ForRead) is LayerTableRecord
								    {
									    IsErased: false, IsEraseStatusToggled: false
								    } layer) layers.Add(layer.Name);
							}
							catch
							{
								// ignore
							}
				}
			}

			return layers;
		}
	}

	/// <summary>
	///     Текстовые стили текущего чертежа
	/// </summary>
	public static List<string> TextStyles
	{
		get
		{
			var textStyles = new List<string>();
			using (Document.LockDocument())
			{
				using (var tr = Database.TransactionManager.StartOpenCloseTransaction())
				{
					var textStyleTable = (TextStyleTable)tr.GetObject(Database.TextStyleTableId, OpenMode.ForRead);
					foreach (var objectId in textStyleTable)
					{
						var txtStl = (TextStyleTableRecord)tr.GetObject(objectId, OpenMode.ForRead);
						if (!textStyles.Contains(txtStl.Name)) textStyles.Add(txtStl.Name);
					}
				}
			}

			return textStyles;
		}
	}

	/// <summary>
	///     Sends a string to the command line in the active Editor
	/// </summary>
	/// <param name="message">The message to send.</param>
	public static void WriteMessage(string message)
	{
		Editor.WriteMessage(message);
	}

	/// <summary>
	///     Sends a string to the command line in the active Editor using String.Format.
	/// </summary>
	/// <param name="message">The message containing format specifications.</param>
	/// <param name="parameter">The variables to substitute into the format string.</param>
	public static void WriteMessage(string message, params object[] parameter)
	{
		Editor.WriteMessage(message, parameter);
	}

	public static void SaveAs(string path)
	{
		Database.SaveAs(path, true, DwgVersion.Current, Database.SecurityParameters);
	}

	public static void UsingTransaction(TransactionDelegate action)
	{
		using var tr = Database.TransactionManager.StartTransaction();
		try
		{
			action(tr);
			tr.Commit();
		}
		catch (Exception)
		{
			tr.Abort();
		}
	}

	public static void UsingModelSpace(this Database database, Action<Transaction, BlockTableRecord> action)
	{
		database.UsingTableRecord(BlockTableRecord.ModelSpace, action);
	}

	public static void UsingTableRecord(this Database database, string tableName,
		Action<Transaction, BlockTableRecord> action)
	{
		UsingTransaction(
			tr =>
			{
				// Get the block table for the current database
				var blockTable =
					(BlockTable) tr.GetObject(
						database.BlockTableId, OpenMode.ForRead);

				// Get the model space block table record
				var tableRecord =
					(BlockTableRecord) tr.GetObject(
						blockTable[tableName],
						OpenMode.ForRead);

				action(tr, tableRecord);
			});
	}


	/// <summary>Применение стиля к блоку согласно стиля</summary>
	/// <param name="blkRefObjectId">ObjectId of block reference</param>
	/// <param name="layerName">Имя слоя</param>
	/// <param name="layerXmlData">Данные слоя</param>
	public static void SetLayerByName(ObjectId blkRefObjectId, string layerName, XElement layerXmlData)
	{
		if (blkRefObjectId == ObjectId.Null) return;

		if (!layerName.Equals("По умолчанию") | !layerName.Equals("Default")) //// "По умолчанию"
		{
			if (LayerUtils.HasLayer(layerName))
				using (Document.LockDocument())
				{
					using (var tr = Database.TransactionManager.StartTransaction())
					{
						var blockReference =
							tr.GetObject(blkRefObjectId, OpenMode.ForWrite, true, true) as BlockReference;
						if (blockReference != null) blockReference.Layer = layerName;

						tr.Commit();
					}
				}
			//else
			//{
			//	if (MainSettings.Instance.IfNoLayer == 1)
			//	{
			//		if (LayerUtils.AddLayerFromXElement(layerXmlData))
			//		{
			//			using (Document.LockDocument())
			//			{
			//				using (var tr = Database.TransactionManager.StartTransaction())
			//				{
			//					var blockReference =
			//						tr.GetObject(blkRefObjectId, OpenMode.ForWrite, true, true) as BlockReference;
			//					if (blockReference != null)
			//					{
			//						blockReference.Layer = layerName;
			//					}

			//					tr.Commit();
			//				}
			//			}
			//		}
			//	}
			//}
		}
		else
		{
			if (Database.Clayer != ObjectId.Null && blkRefObjectId != ObjectId.Null)
				using (var tr = Database.TransactionManager.StartTransaction())
				{
					var blockReference = tr.GetObject(blkRefObjectId, OpenMode.ForWrite, true, true) as BlockReference;
					var layer = tr.GetObject(Database.Clayer, OpenMode.ForRead) as LayerTableRecord;
					if (blockReference != null) blockReference.Layer = layer?.Name;

					tr.Commit();
				}
		}
	}

	/// <summary>
	///     Написать сообщение в командную строку при сборке в Debug
	/// </summary>
	/// <param name="message">Сообщение</param>
	public static void WriteMessageInDebug(string message)
	{
#if DEBUG
		Editor?.WriteMessage($"\n{message}");
#endif
	}

	/// <summary>Получить имя типа линии по ObjectId</summary>
	/// <param name="ltid">ObjectId</param>
	/// <returns></returns>
	public static string GetLineTypeName(ObjectId ltid)
	{
		var lt = "Continuous";
		if (ltid == ObjectId.Null) return lt;

		using (Document.LockDocument())
		{
			using (var tr = Database.TransactionManager.StartTransaction())
			{
				var linetype = tr.GetObject(ltid, OpenMode.ForRead) as LinetypeTableRecord;
				lt = linetype?.Name;
				tr.Commit();
			}
		}

		return lt;
	}

	/// <summary>Получить ObjectId типа линии по имени в текущем документе</summary>
	/// <param name="lineTypeName">Имя типа линии</param>
	/// <returns></returns>
	public static ObjectId GetLineTypeObjectId(string lineTypeName)
	{
		var lineTypeObjectId = ObjectId.Null;
		if (string.IsNullOrEmpty(lineTypeName)) return ObjectId.Null;

		using (Document.LockDocument())
		{
			using (var tr = Database.TransactionManager.StartTransaction())
			{
				var linetypeTable = tr.GetObject(Database.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
				if (linetypeTable != null)
				{
					if (linetypeTable.Has(lineTypeName))
					{
						lineTypeObjectId = linetypeTable[lineTypeName];
					}
					else
					{
						const string filename = "acad.lin";
						try
						{
							var path = HostApplicationServices.Current.FindFile(
								filename, Database, FindFileHint.Default);
							Database.LoadLineTypeFile(lineTypeName, path);
							lineTypeObjectId = linetypeTable[lineTypeName];
						}
						catch (Autodesk.AutoCAD.Runtime.Exception exception)
						{
							if (exception.ErrorStatus == ErrorStatus.FilerError)
							{
								// Не удалось найти файл
								MessageBox.Show($"{"err1"}MessageBoxIcon.Warning");
							}
							else if (exception.ErrorStatus == ErrorStatus.DuplicateRecordName)
							{
								// ignore
							}
							else
							{
								// Не удалось загрузить тип линий
								MessageBox.Show($"Не удалось загрузить тип линий: {lineTypeName}");
							}
						}
					}
				}

				tr.Commit();
			}
		}

		return lineTypeObjectId;
	}


	/// <summary>
	///     Установка типа линии для блока согласно стиля
	/// </summary>
	/// <param name="blkRefObjectId">Идентификатор блока</param>
	/// <param name="lineTypeName">Имя типа линии</param>
	public static void SetLineType(ObjectId blkRefObjectId, string lineTypeName)
	{
		if (blkRefObjectId == ObjectId.Null) return;

		using (Document.LockDocument())
		{
			using (var tr = Document.TransactionManager.StartTransaction())
			{
				var blockReference = tr.GetObject(blkRefObjectId, OpenMode.ForWrite, true, true) as BlockReference;
				if (blockReference != null)
				{
					if (HasLineType(lineTypeName, tr))
					{
						blockReference.Linetype = lineTypeName;
					}
					else
					{
						if (LoadLineType(lineTypeName)) blockReference.Linetype = lineTypeName;
					}
				}

				tr.Commit();
			}
		}
	}

	/// <summary>Проверка наличия типа линии в документе</summary>
	private static bool HasLineType(string lineTypeName, Transaction tr)
	{
		var lineTypeTable = tr.GetObject(Database.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;
		if (lineTypeTable != null) return lineTypeTable.Has(lineTypeName);

		return false;
	}

	/// <summary>Загрузка типа линии в файл</summary>
	/// <param name="lineTypeName">Имя типа линии</param>
	/// <param name="fileNames">Файлы, в которых искать тип линии. Если не указаны, то из стандартного</param>
	public static bool LoadLineType(string lineTypeName, List<string> fileNames = null)
	{
		var loaded = false;
		if (fileNames != null)
			foreach (var fileName in fileNames)
				try
				{
					Database.LoadLineTypeFile(lineTypeName, fileName);
					loaded = true;
					break;
				}
				catch
				{
					// ignore
				}

		if (!loaded)
			try
			{
				Database.LoadLineTypeFile(lineTypeName, "acad.lin");
				loaded = true;
			}
			catch
			{
				// ignore
			}

		return loaded;
	}

	/// <summary>
	///     Возвращает идентификатор текстового стиля по имени
	/// </summary>
	/// <param name="textStyleName">Имя текстового стиля</param>
	/// <returns>
	///     <see cref="ObjectId" />
	/// </returns>
	public static ObjectId GetTextStyleIdByName(string textStyleName)
	{
		using (Document.LockDocument())
		{
			using (var tr = Database.TransactionManager.StartOpenCloseTransaction())
			{
				var textStyleTable = (TextStyleTable)tr.GetObject(Database.TextStyleTableId, OpenMode.ForRead);
				foreach (var objectId in textStyleTable)
				{
					var txtStl = (TextStyleTableRecord)tr.GetObject(objectId, OpenMode.ForRead);
					if (txtStl.Name.Equals(textStyleName)) return objectId;
				}
			}
		}

		return ObjectId.Null;
	}
}