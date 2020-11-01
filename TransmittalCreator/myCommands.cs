// (C) Copyright 2020 by HP Inc. 
//
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using DV2177.Common;
using TransmittalCreator.ViewModel;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(TransmittalCreator.MyCommands))]

namespace TransmittalCreator
{

    // This class is instantiated by AutoCAD for each document when
    // a command is called by the user the first time in the context
    // of a given document. In other words, non static data in this class
    // is implicitly per-document!
    public class MyCommands : Utils, IExtensionApplication
    {
        // The CommandMethod attribute can be applied to any public  member 
        // function of any public class.
        // The function should take no arguments and return nothing.
        // If the method is an intance member then the enclosing class is 
        // intantiated for each document. If the member is a static member then
        // the enclosing class is NOT intantiated.
        //
        // NOTE: CommandMethod has overloads where you can provide helpid and
        // context menu.

        // Modal Command with localized name
        [CommandMethod("MyGroup", "MyCommand", "MyCommandLocal", CommandFlags.Modal)]
        public void MyCommand() // This method can have any name
        {
            // Put your command code here
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed;
            if (doc != null)
            {
                ed = doc.Editor;
                ed.WriteMessage("Hello, this is your first command.");

            }
        }

        // Modal Command with pickfirst selection
        [CommandMethod("MyGroup", "MyPickFirst", "MyPickFirstLocal", CommandFlags.Modal | CommandFlags.UsePickSet)]
        public void MyPickFirst() // This method can have any name
        {
            PromptSelectionResult result = Application.DocumentManager.MdiActiveDocument.Editor.GetSelection();
            if (result.Status == PromptStatus.OK)
            {
                // There are selected entities
                // Put your command using pickfirst set code here
            }
            else
            {
                // There are no selected entities
                // Put your command code here
            }
        }

        // Application Session Command with localized name
        [CommandMethod("MyGroup", "MySessionCmd", "MySessionCmdLocal", CommandFlags.Modal | CommandFlags.Session)]
        public void MySessionCmd() // This method can have any name
        {
            // Put your command code here
        }

        // LispFunction is similar to CommandMethod but it creates a lisp 
        // callable function. Many return types are supported not just string
        // or integer.
        [LispFunction("MyLispFunction", "MyLispFunctionLocal")]
        public int MyLispFunction(ResultBuffer args) // This method can have any name
        {
            // Put your command code here

            // Return a value to the AutoCAD Lisp Interpreter
            return 1;
        }

        [CommandMethod("Cttransm")]
        public void ListAttributes()
        {
            Dictionary<string, string> attrList = new Dictionary<string, string>();

            MainWindow window = new MainWindow(new BlockViewModel(attrList));

            Application.ShowModalWindow(window);

        }

        [CommandMethod("CalcBlocks1")]
        static public void CalcBlocks1()
        {
            Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
            Database db = HostApplicationServices.WorkingDatabase;
            PromptResult res = ed.GetString("\nType name of block: ");
            if (res.Status == PromptStatus.OK)
            {
                TypedValue[] flt =
                {
new TypedValue(0, "INSERT"),
new TypedValue(2, res.StringResult),
new TypedValue(410, "Model")
};
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


        /// <summary>
        /// Summary description for Class.
        /// </summary>

        // Define Command "CalcBlocks"
        [CommandMethod("CalcBlocks")]
        //static public void CalcBlocks()
        //{
        //    Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        //    Database db = HostApplicationServices.WorkingDatabase;
        //    PromptResult res = ed.GetString("\nType name of block: ");
        //    if (res.Status == PromptStatus.OK)
        //    {
        //        ObjectIdCollection ids = GetAllBlockReferenceByName(res.StringResult, db);
        //        if (ids != null)
        //        {
        //            ed.WriteMessage("\nNumber of blocks with name <{0}> in Model Space is {1}", res.StringResult, ids.Count);
        //        }
        //        else
        //        {
        //            ed.WriteMessage("\nNo blocks with name <{0}>", res.StringResult);
        //        }
        //    }
        //}

        static public List<ObjectId> GetAllBlockReferenceByName(string name, Database db)
        {
            ObjectIdCollection ids_temp = null, ids = new ObjectIdCollection();
            List<ObjectId> objectIds = new List<ObjectId>();
            Transaction tr = db.TransactionManager.StartTransaction();
            try
            {
                BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[name], OpenMode.ForRead);
                BlockTableRecord btr_model = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);
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

            return objectIds;
        }



        [CommandMethod("blockName")]
        static public void GetBlockName()

        {

            Document doc = Application.DocumentManager.MdiActiveDocument;

            Database db = doc.Database;

            Editor ed = doc.Editor;

            PromptEntityOptions options =

                new PromptEntityOptions("\nSelect block reference");

            options.SetRejectMessage("\nSelect only block reference");

            options.AddAllowedClass(typeof(BlockReference), false);



            PromptEntityResult acSSPrompt = ed.GetEntity(options);



            using (Transaction tx =

                db.TransactionManager.StartTransaction())

            {

                BlockReference blockRef = tx.GetObject(acSSPrompt.ObjectId,

                    OpenMode.ForRead) as BlockReference;



                BlockTableRecord block = null;

                if (blockRef.IsDynamicBlock)

                {
                    //get the real dynamic block name.

                    block = tx.GetObject(blockRef.DynamicBlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                }
                else
                {

                    block = tx.GetObject(blockRef.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;

                }

                if (block != null)

                {

                    ed.WriteMessage("Block name is : "

                                    + block.Name + "\n");

                }

                tx.Commit();

            }
        }

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Terminate()
        {
            throw new NotImplementedException();
        }
    }

}
