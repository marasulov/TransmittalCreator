using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using DV2177.Common;

namespace TransmittalCreator
{
    class Archive
    {
        [CommandMethod("CalcBlocks1")]
        static public void CalcBlocks1()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = HostApplicationServices.WorkingDatabase;
            PromptResult res = ed.GetString("\nType name of block: ");

            if (res.Status == PromptStatus.OK)
            {
                TypedValue[] flt = { new TypedValue(0, "INSERT"), new TypedValue(2, res.StringResult), new TypedValue(410, "Model") };

                PromptSelectionResult rs = ed.SelectAll(new SelectionFilter(flt));
                if (rs.Status == PromptStatus.OK && rs.Value.Count > 0)
                {
                    ed.WriteMessage("\nNumber of blocks with name <{0}> in Model Space is {1}", res.StringResult, rs.Value.Count);
                }
                else
                {
                    ed.WriteMessage("\nNo blocks with name <{0}>", res.StringResult);
                }
            }
        }

        [CommandMethod("filterblocks")]
        public void FilterBlocks()
        {
            Document doc = Active.Document;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            PromptEntityOptions opts = new PromptEntityOptions("\nSelect a block: ");
            opts.SetRejectMessage("Only a block.");
            opts.AddAllowedClass(typeof(BlockReference), true);
            PromptEntityResult per = ed.GetEntity(opts);
            if (per.Status != PromptStatus.OK)
                return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockReference br = (BlockReference)tr.GetObject(per.ObjectId, OpenMode.ForRead);
                blockName = GetEffectiveName(br);
                TypedValue[] filterList = new TypedValue[3]
                {
                        new TypedValue(0, "INSERT"),
                        new TypedValue(2, "`*U*," + blockName),
                        new TypedValue(8, br.Layer)
                };
                ed.SelectionAdded += onSelectionAdded;
                PromptSelectionResult psr = ed.SelectAll(new SelectionFilter(filterList));
                ed.SelectionAdded -= onSelectionAdded;
                ed.SetImpliedSelection(psr.Value);
                ed.WriteMessage("\nNumber of selected objects: {0}", psr.Value.Count);
                tr.Commit();
            }
        }

        private string blockName;

        private string GetEffectiveName(BlockReference br)
        {
            return br.IsDynamicBlock ?
                ((BlockTableRecord)br.DynamicBlockTableRecord.GetObject(OpenMode.ForRead)).Name :
                br.Name;
        }

        void onSelectionAdded(object sender, SelectionAddedEventArgs e)
        {
            for (int i = 0; i < e.AddedObjects.Count; i++)
            {
                BlockReference br = (BlockReference)e.AddedObjects[i].ObjectId.GetObject(OpenMode.ForRead);
                if (GetEffectiveName(br) != blockName)
                    e.Remove(i);
            }
        }

        /// <summary>
        /// Summary description for Class.
        /// </summary>

        // Define Command "CalcBlocks"
        [CommandMethod("CalcBlocks")]
        static public void CalcBlocks()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = HostApplicationServices.WorkingDatabase;
            var pso = new PromptStringOptions("\nName of dynamic block to search for");
            pso.AllowSpaces = true;
            var res = ed.GetString(pso);
            if (res.Status == PromptStatus.OK)
            {

                ObjectIdCollection ids = GetAllBlockReferenceByName(res.StringResult, db);
                if (ids != null)
                {
                    ed.WriteMessage("\nNumber of blocks with name <{0}> in Model Space is {1}", res.StringResult, ids.Count);
                }
                else
                {
                    ed.WriteMessage("\nNo blocks with name <{0}>", res.StringResult);
                }
            }
        }

        public static ObjectIdCollection GetAllBlockReferenceByName(string name, Database db)
        {
            ObjectIdCollection ids_temp = null, ids = new ObjectIdCollection();
            List<string> blkNames = new List<string>();
            blkNames.Add(name);
            List<ObjectId> objectIds = new List<ObjectId>();
            Transaction tr = db.TransactionManager.StartTransaction();
            try
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[name], OpenMode.ForRead);
                BlockTableRecord btr_model = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

                SelectionFilter sf = new SelectionFilter(CreateFilterListForBlocks(blkNames));
                PromptSelectionResult psr = Active.Editor.SelectAll(sf);

                ObjectId id_model = btr_model.ObjectId;
                ids_temp = btr.GetBlockReferenceIds(true, true);
                foreach (ObjectId id in ids_temp)
                {
                    DBObject obj = (DBObject)tr.GetObject(id, OpenMode.ForRead);
                    // If BlockReference owned with Model Space - add it to collection
                    if (obj.OwnerId == id_model)
                    {
                        ids.Add(id);
                        objectIds.Add(id);
                    }
                }
                tr.Commit();
            }
            finally
            {
                tr.Dispose();
            }

            return ids;
        }

        public static ObjectIdCollection GetBlockByName(string blkName)
        {
            ObjectIdCollection ids_temp = null, ids = new ObjectIdCollection();

            Editor ed = Active.Editor;
            Document doc = Active.Document;
            List<string> blkNames = new List<string>();
            blkNames.Add(blkName);
            var tr = doc.TransactionManager.StartTransaction();
            using (tr)
            {
                var bt = (BlockTable)tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead);

                // Start by getting access to our block, if it exists
                if (!bt.Has(blkName))
                {
                    ed.WriteMessage(
                        "\nCannot find block called \"{0}\".", blkName
                    );

                }
                // Get the anonymous block names
                var btr =
                    (BlockTableRecord)tr.GetObject(
                        bt[blkName], OpenMode.ForRead
                    );
                if (!btr.IsDynamicBlock)
                {
                    ed.WriteMessage(
                        "\nCannot find a dynamic block called \"{0}\".", blkName
                    );

                }
                // Get the anonymous blocks and add them to our list
                var anonBlks = btr.GetAnonymousBlockIds();
                foreach (ObjectId bid in anonBlks)
                {
                    var btr2 =
                        (BlockTableRecord)tr.GetObject(bid, OpenMode.ForRead);
                    blkNames.Add(btr2.Name);
                }
                tr.Commit();
            }
            // Build a conditional filter list so that only
            // entities with the specified properties are
            // selected
            SelectionFilter sf =
                new SelectionFilter(CreateFilterListForBlocks(blkNames));
            PromptSelectionResult psr = ed.SelectAll(sf);
            ed.WriteMessage(
                "\nFound {0} entit{1}.",
                psr.Value.Count,
                (psr.Value.Count == 1 ? "y" : "ies"));
            return ids;
        }

        [CommandMethod("SDB")]
        static public void SelectDynamicBlocks()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var pso =
                new PromptStringOptions(
                    "\nName of dynamic block to search for"
                );
            pso.AllowSpaces = true;
            var pr = ed.GetString(pso);
            if (pr.Status != PromptStatus.OK)
                return;
            string blkName = pr.StringResult;
            List<string> blkNames = new List<string>();
            blkNames.Add(blkName);
            var tr = doc.TransactionManager.StartTransaction();
            using (tr)
            {
                var bt =
                    (BlockTable)tr.GetObject(
                        doc.Database.BlockTableId,
                        OpenMode.ForRead
                    );
                // Start by getting access to our block, if it exists
                if (!bt.Has(blkName))
                {
                    ed.WriteMessage(
                        "\nCannot find block called \"{0}\".", blkName
                    );
                    return;
                }
                // Get the anonymous block names
                var btr =
                    (BlockTableRecord)tr.GetObject(
                        bt[blkName], OpenMode.ForRead
                    );
                if (!btr.IsDynamicBlock)
                {
                    ed.WriteMessage(
                        "\nCannot find a dynamic block called \"{0}\".", blkName
                    );
                    return;
                }
                // Get the anonymous blocks and add them to our list
                var anonBlks = btr.GetAnonymousBlockIds();
                foreach (ObjectId bid in anonBlks)
                {
                    var btr2 =
                        (BlockTableRecord)tr.GetObject(bid, OpenMode.ForRead);
                    blkNames.Add(btr2.Name);
                }
                tr.Commit();
            }
            // Build a conditional filter list so that only
            // entities with the specified properties are
            // selected
            SelectionFilter sf =
                new SelectionFilter(CreateFilterListForBlocks(blkNames));
            PromptSelectionResult psr = ed.SelectAll(sf);
            ed.WriteMessage(
                "\nFound {0} entit{1}.",
                psr.Value.Count,
                (psr.Value.Count == 1 ? "y" : "ies")
            );
        }

        private static TypedValue[] CreateFilterListForBlocks(List<string> blkNames)
        {
            // If we don't have any block names, return null
            if (blkNames.Count == 0)
                return null;
            // If we only have one, return an array of a single value
            if (blkNames.Count == 1)
                return new TypedValue[]
                {
                    new TypedValue((int)DxfCode.BlockName, blkNames[0])
                };
            // We have more than one block names to search for...
            // Create a list big enough for our block names plus
            // the containing "or" operators
            List<TypedValue> tvl = new List<TypedValue>(blkNames.Count + 2);
            // Add the initial operator
            tvl.Add(new TypedValue((int)DxfCode.Operator, "<or"));
            // Add an entry for each block name, prefixing the
            // anonymous block names with a reverse apostrophe
            foreach (var blkName in blkNames)
            {
                tvl.Add(
                    new TypedValue(
                        (int)DxfCode.BlockName,
                        (blkName.StartsWith("*") ? "`" + blkName : blkName)
                    )
                );
            }
            // Add the final operator
            tvl.Add(
                new TypedValue(
                    (int)DxfCode.Operator,
                    "or>"
                )
            );
            // Return an array from the list
            return tvl.ToArray();
        }
    }
}
