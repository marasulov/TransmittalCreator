using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using TransmittalCreator.DBCad;

//[assembly: CommandClass(typeof(BlockExtCommand))]
namespace TransmittalCreator.CadCommands
{

	public static class BlockExtCommand
	{
		
		//[CommandMethod("BlockExt")]
		public static void BlockExt()
		{
			if (Active.Document == null) return;
			
			PromptEntityOptions enOpt =
				new PromptEntityOptions("\nВыберите вставку блока:");
			enOpt.SetRejectMessage("Это не вставка блока");
			enOpt.AddAllowedClass(typeof(BlockReference), true);
			PromptEntityResult enRes = Active.Editor.GetEntity(enOpt);
			if (enRes.Status == PromptStatus.OK)
			{
				Extents3d blockExt = new Extents3d(Point3d.Origin, Point3d.Origin);
				using (BlockReference bref = enRes.ObjectId.Open(OpenMode.ForRead) as BlockReference)
				{
					Matrix3d mat = bref.BlockTransform;
					using (BlockTableRecord btr = bref.BlockTableRecord.Open(OpenMode.ForRead) as BlockTableRecord)
					{
						foreach (ObjectId id in btr)
						{
							using (DBObject obj = id.Open(OpenMode.ForRead) as DBObject)
							{
								Entity en = obj as Entity;
								if (en != null && en.Visible == true)
								{

									try
									{
										Extents3d ext = en.GeometricExtents;
										ext.TransformBy(mat);
										if (blockExt.MinPoint == Point3d.Origin && blockExt.MaxPoint == Point3d.Origin)
											blockExt = ext;
										else
											blockExt.AddExtents(ext);
									}
									catch (Autodesk.AutoCAD.Runtime.Exception e)
									{
										Active.Editor.WriteMessage(e.Message);

									}


								}
							}
						}
					}
				}
				string s =
					"MinPoint: " + blockExt.MinPoint.ToString() + " " +
					"MaxPoint: " + blockExt.MaxPoint.ToString();
				Active.Editor.WriteMessage(s);
			}
		}
	}
}
