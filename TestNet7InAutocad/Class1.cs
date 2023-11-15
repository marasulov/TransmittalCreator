using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace TestNet7InAutocad
{
	public class Class1
	{
		[CommandMethod("BlockExt")]
		public static void BlockExt()
		{
			var doc = Application.DocumentManager.MdiActiveDocument;
			var ed = doc.Editor;
			if (doc == null) return;

			var enOpt =
				new PromptEntityOptions("\nВыберите вставку блока:");
			enOpt.SetRejectMessage("Это не вставка блока");
			enOpt.AddAllowedClass(typeof(BlockReference), true);
			var enRes = ed.GetEntity(enOpt);
			if (enRes.Status == PromptStatus.OK)
			{
				var blockExt = new Extents3d(Point3d.Origin, Point3d.Origin);
				using (var bref = enRes.ObjectId.Open(OpenMode.ForRead) as BlockReference)
				{
					var mat = bref.BlockTransform;
					using (var btr = bref.BlockTableRecord.Open(OpenMode.ForRead) as BlockTableRecord)
					{
						foreach (var id in btr)
							using (var obj = id.Open(OpenMode.ForRead))
							{
								var en = obj as Entity;
								if (en != null && en.Visible)
									try
									{
										var ext = en.GeometricExtents;
										ext.TransformBy(mat);
										if (blockExt.MinPoint == Point3d.Origin && blockExt.MaxPoint == Point3d.Origin)
											blockExt = ext;
										else
											blockExt.AddExtents(ext);
									}
									catch (Autodesk.AutoCAD.Runtime.Exception e)
									{
										ed.WriteMessage(e.Message);
									}
							}
					}
				}

				var s =
					"MinPoint: " + blockExt.MinPoint + " " +
					"MaxPoint: " + blockExt.MaxPoint;
				ed.WriteMessage(s);
			}
		}


	}
}