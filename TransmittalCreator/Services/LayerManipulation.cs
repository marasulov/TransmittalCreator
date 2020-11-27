using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;

[assembly: PerDocumentClass(typeof(TransmittalCreator.Services.UnlockCommands))]
namespace TransmittalCreator.Services
{
    public class UnlockCommands : System.IDisposable
    {
        public const string RecordName = "TtifLayerUnlockData";
        // Map commands to layers to unlock and relock
        public Dictionary<string, ObjectIdCollection> CommandMap =
          new Dictionary<string, ObjectIdCollection>();
        private Document _doc = null;
        private bool _disposed = false;
        // Constructor
        public UnlockCommands(Document doc)
        {
            _doc = doc;
            doc.UserData.Add(RecordName, this);
            // Add our command handlers
            doc.CommandWillStart += OnCommandWillStart;
            doc.CommandEnded += OnCommandEnded;
            doc.CommandCancelled += OnCommandEnded;
            doc.CommandFailed += OnCommandEnded;
        }
        void OnCommandWillStart(object sender, CommandEventArgs e)
        {
            var doc = (Document)sender;
            var uc = doc.UserData[UnlockCommands.RecordName] as UnlockCommands;
            // If the command ending is in our list, unlock the layer(s)
            if (uc != null && uc.CommandMap.ContainsKey(e.GlobalCommandName))
            {
                doc.LockOrUnlockLayers(false, uc.CommandMap[e.GlobalCommandName], false);
            }
        }
        void OnCommandEnded(object sender, CommandEventArgs e)
        {
            var doc = (Document)sender;
            var uc = doc.UserData[UnlockCommands.RecordName] as UnlockCommands;
            // If the command ending is in our list, relock the layer(s)
            if (uc != null && uc.CommandMap.ContainsKey(e.GlobalCommandName))
            {
                doc.LockOrUnlockLayers(true, uc.CommandMap[e.GlobalCommandName], false);
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            // Remove our command handlers
            _doc.CommandWillStart -= OnCommandWillStart;
            _doc.CommandEnded -= OnCommandEnded;
            _doc.CommandCancelled -= OnCommandEnded;
            _doc.CommandFailed += OnCommandEnded;
            _disposed = true;
        }
    }

    public static class Extensions
    {
        public static void LockOrUnlockLayers(this Document doc, bool dolock,
            ObjectIdCollection layers = null, bool ignoreCurrent = true,
            bool lockZero = false
        )
        {
            var db = doc.Database;
            var ed = doc.Editor;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                var layerIds =
                  (layers != null ? (IEnumerable)layers :
                    (IEnumerable)tr.GetObject(db.LayerTableId, OpenMode.ForRead));
                foreach (ObjectId ltrId in layerIds)
                {
                    // Don't try to lock/unlock either the current layer or layer 0
                    // (depending on whether lockZero == true for the latter)
                    if (
                      (!ignoreCurrent || ltrId != db.Clayer) &&
                      (lockZero || ltrId != db.LayerZero)
                    )
                    {
                        // Open the layer for write and lock/unlock it
                        var ltr = (LayerTableRecord)tr.GetObject(ltrId, OpenMode.ForWrite);
                        ltr.IsLocked = dolock;
                        ltr.IsOff = ltr.IsOff; // This is needed to force a graphics update
                    }
                }
                tr.Commit();
            }
            // These two calls will result in the layer's geometry fading/unfading
            // appropriately
            ed.ApplyCurDwgLayerTableChanges();
            ed.Regen();
        }
        public static ObjectIdCollection SelectLayers(this Document doc)
        {
            var db = doc.Database;
            var ed = doc.Editor;
            // A list of the layers' names & IDs contained
            // in the current database, sorted by layer name
            var ld = new SortedList<string, ObjectId>();
            // A list of the selected layers' IDs
            var lids = new ObjectIdCollection();
            // Start by populating the list of names/IDs
            // from the LayerTable
            using (var tr = db.TransactionManager.StartOpenCloseTransaction())
            {
                var lt = (LayerTable)tr.GetObject(db.LayerTableId, OpenMode.ForRead);
                foreach (ObjectId lid in lt)
                {
                    var ltr = (LayerTableRecord)tr.GetObject(lid, OpenMode.ForRead);
                    ld.Add(ltr.Name, lid);
                }
            }
            // Display a numbered list of the available layers
            ed.WriteMessage("\nLayers available:");
            ed.SelectNamedLayers(ld, lids);
            return lids;
        }
        private static void SelectNamedLayers(
          this Editor ed,
          SortedList<string, ObjectId> ld,
          ObjectIdCollection lids
        )
        {
            int i = 1;
            foreach (KeyValuePair<string, ObjectId> kv in ld)
            {
                ed.WriteMessage("\n{0} - {1}", i++, kv.Key);
            }
            // We will ask the user to select from the list
            var pio = new PromptIntegerOptions("\nEnter number of layer to add: ");
            pio.LowerLimit = 1;
            pio.UpperLimit = ld.Count;
            pio.AllowNone = true;
            // And will do so in a loop, waiting for Escape or Enter to terminate
            PromptIntegerResult pir;
            do
            {
                // Select one from the list
                pir = ed.GetInteger(pio);
                if (pir.Status == PromptStatus.OK)
                {
                    // Get the layer's name
                    string ln = ld.Keys[pir.Value - 1];
                    // And then its ID
                    ObjectId lid;
                    ld.TryGetValue(ln, out lid);
                    // Add the layer'd ID to the list, if it's not already on it
                    if (lids.Contains(lid))
                    {
                        ed.WriteMessage("\nLayer \"{0}\" has already been selected.", ln);
                    }
                    else
                    {
                        lids.Add(lid);
                        ed.WriteMessage("\nAdded \"{0}\" to selected layers.", ln);
                    }
                }
            } while (pir.Status == PromptStatus.OK);
        }
    }
}
