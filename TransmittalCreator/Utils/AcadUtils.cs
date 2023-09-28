using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;
using System.Xml.Linq;
using Autodesk.AutoCAD.Geometry;
//using ModPlusAPI;
//using ModPlusAPI.Windows;
using AcApp = Autodesk.AutoCAD.ApplicationServices.Core.Application;
using MessageBox = System.Windows.MessageBox;

namespace TransmittalCreator.Utils
{
	/// <summary>
	/// Различные утилиты работы с AutoCAD
	/// </summary>
	public static class AcadUtils
	{
		/// <summary>
		/// БД активного документа
		/// </summary>
		public static Database Database => HostApplicationServices.WorkingDatabase;

		/// <summary>
		/// Коллекция документов
		/// </summary>
		public static DocumentCollection Documents => AcApp.DocumentManager;

		/// <summary>
		/// Возвращает коллекцию аннотационных масштабов текущей БД чертежа
		/// </summary>
		public static ObjectContextCollection ObjectContextCollection
		{
			get
			{
				var ocm = Database.ObjectContextManager;
				return ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
			}
		}

	
		/// <summary>
		/// Список масштабов текущего чертежа
		/// </summary>
		public static List<string> Scales
		{
			get
			{
				var scales = new List<string>();
				var ocm = Database.ObjectContextManager;
				if (ocm != null)
				{
					var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
					foreach (var objectContext in occ)
					{
						scales.Add(((AnnotationScale)objectContext).Name);
					}
				}

				return scales;
			}
		}

#if DEBUG
		
#endif

		/// <summary>
		/// Текущий аннотативный масштаб
		/// </summary>
		public static AnnotationScale GetCurrentScale()
		{
			return ObjectContextCollection.CurrentContext as AnnotationScale;
		}

		

		/// <summary>
		/// Открыть объект для чтения
		/// </summary>
		/// <typeparam name="T">Тип объекта</typeparam>
		/// <param name="objectId">Идентификатор примитива</param>
		/// <param name="openErased">Открыть с меткой "Стерто"</param>
		/// <param name="forceOpenOnLockedLayer">Открыть объект на заблокированном слое</param>
		public static T Read<T>(this ObjectId objectId, bool openErased = true, bool forceOpenOnLockedLayer = true)
			where T : DBObject
		{
			return objectId.GetObject(0, openErased, forceOpenOnLockedLayer) as T;
		}

		/// <summary>
		/// Открыть объект для записи
		/// </summary>
		/// <typeparam name="T">Тип объекта</typeparam>
		/// <param name="objectId">Идентификатор примитива</param>
		/// <param name="openErased">Открыть с меткой "Стерто"</param>
		/// <param name="forceOpenOnLockedLayer">Открыть объект на заблокированном слое</param>
		public static T Write<T>(this ObjectId objectId, bool openErased = true, bool forceOpenOnLockedLayer = true)
			where T : DBObject
		{
			return objectId.GetObject(OpenMode.ForWrite, openErased, forceOpenOnLockedLayer) as T;
		}

		/// <summary>
		/// Получение аннотативного масштаба по имени из текущего чертежа
		/// </summary>
		/// <param name="name">Имя масштаба</param>
		/// <returns>Аннотативный масштаб с таким именем или текущий масштаб в БД</returns>
		public static AnnotationScale GetAnnotationScaleByName(string name)
		{
			var ocm = Database.ObjectContextManager;
			if (ocm != null && !string.IsNullOrEmpty(name))
			{
				var occ = ocm.GetContextCollection("ACDB_ANNOTATIONSCALES");
				if (occ != null)
				{
					foreach (var objectContext in occ)
					{
						var asc = objectContext as AnnotationScale;
						if (asc?.Name == name)
						{
							return asc;
						}
					}
				}
			}

			return Database.Cannoscale;
		}

		/// <summary>
		/// Возвращает численный масштаб (отношение единиц чертежа к единицам листа)
		/// </summary>
		/// <param name="scale">Аннотативный масштаб</param>
		public static double GetNumericScale(this AnnotationScale scale)
		{
			return scale.DrawingUnits / scale.PaperUnits;
		}

		/// <summary>
		/// Найти все блоки в текущем пространстве (модель/лист), представляющие интеллектуальный примитив указанного типа
		/// </summary>
		/// <typeparam name="T">Допустимый тип</typeparam>
		/// <param name="entityType">Тип интеллектуального объекта для поиска</param>
		//public static List<T> GetAllIntellectualEntitiesInCurrentSpace<T>(Type entityType)
		//	where T : SmartEntity
		//{
		//	var list = new List<T>();
		//	var appName = $"mp{entityType.Name}";
		//	using (var tr = Database.TransactionManager.StartOpenCloseTransaction())
		//	{
		//		var blockTableRecord = (BlockTableRecord)tr.GetObject(Database.CurrentSpaceId, OpenMode.ForRead);
		//		foreach (var objectId in blockTableRecord)
		//		{
		//			if (objectId == ObjectId.Null)
		//			{
		//				continue;
		//			}

		//			if (tr.GetObject(objectId, OpenMode.ForRead) is BlockReference blockReference)
		//			{
		//				var xData = blockReference.GetXDataForApplication(appName);
		//				if (xData != null)
		//				{
		//					list.Add(EntityReaderService.Instance.GetFromEntity<T>(blockReference));
		//				}
		//			}
		//		}

		//		tr.Commit();
		//	}

		//	return list;
		//}

		/// <summary>
		/// Парсинг аннотативного масштаба из строки
		/// </summary>
		/// <param name="str">Строка с масштабом</param>
		public static AnnotationScale AnnotationScaleFromString(string str)
		{
			var defaultScale = new AnnotationScale { Name = "1:1", DrawingUnits = 1.0, PaperUnits = 1.0 };
			var splitted = str.Split(':');
			if (splitted.Length == 2)
			{
				var scale = new AnnotationScale
				{
					Name = str,
					PaperUnits = double.TryParse(splitted[0], out var d) ? d : 1.0,
					DrawingUnits = double.TryParse(splitted[1], out d) ? d : 1.0
				};
				return scale;
			}

			return defaultScale;
		}

		/// <summary>
		/// Находится ли текущий документ в пространстве модели
		/// </summary>
		public static bool IsInModel()
		{
			// https://spiderinnet1.typepad.com/blog/2014/05/autocad-net-detect-current-space-model-or-paper-and-viewport.html
			return AcApp.DocumentManager.MdiActiveDocument.Database.TileMode;
		}

		/// <summary>
		/// Зуммирует объекты в файле по границам
		/// </summary>
		/// <param name="fileName">Имя файла</param>
		/// <returns></returns>
		public static string ZoomFilesAndSave(string fileName)
		{
			string newFileName = "";
			using (Database db = new Database(false, false))
			{
				db.ReadDwgFile(fileName, FileOpenMode.OpenForReadAndReadShare, true, null);
				Database prevDb = HostApplicationServices.WorkingDatabase;
				HostApplicationServices.WorkingDatabase = db;
				db.UpdateExt(true);
				using (ViewportTable vTab = db.ViewportTableId.GetObject(OpenMode.ForRead) as ViewportTable)
				{
					ObjectId acVptId = vTab["*Active"];
					using (ViewportTableRecord vpTabRec = acVptId.GetObject(OpenMode.ForWrite) as ViewportTableRecord)
					{
						double scrRatio = (vpTabRec.Width / vpTabRec.Height);
						Matrix3d matWCS2DCS = Matrix3d.PlaneToWorld(vpTabRec.ViewDirection);
						matWCS2DCS = Matrix3d.Displacement(vpTabRec.Target - Point3d.Origin) * matWCS2DCS;
						matWCS2DCS = Matrix3d.Rotation(-vpTabRec.ViewTwist,
							             vpTabRec.ViewDirection,
							             vpTabRec.Target)
						             * matWCS2DCS;
						matWCS2DCS = matWCS2DCS.Inverse();
						Extents3d extents = new Extents3d(db.Extmin, db.Extmax);
						extents.TransformBy(matWCS2DCS);
						double width = (extents.MaxPoint.X - extents.MinPoint.X);
						double height = (extents.MaxPoint.Y - extents.MinPoint.Y);
						Point2d center = new Point2d((extents.MaxPoint.X + extents.MinPoint.X) * 0.5,
							(extents.MaxPoint.Y + extents.MinPoint.Y) * 0.5);
						if (width > (height * scrRatio))
							height = width / scrRatio;
						vpTabRec.Height = height;
						vpTabRec.Width = height * scrRatio;
						vpTabRec.CenterPoint = center;
					}
				}

				HostApplicationServices.WorkingDatabase = prevDb;
				newFileName = fileName.Substring(0, fileName.Length - 4) + "z.dwg";
				db.SaveAs(newFileName, DwgVersion.Current);
			}

			return newFileName;
		}
	}
}
