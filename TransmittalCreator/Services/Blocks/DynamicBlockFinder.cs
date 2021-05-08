using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using TransmittalCreator.DBCad;
using TransmittalCreator.Models.Layouts;

namespace TransmittalCreator.Services.Blocks
{
    public class DynamicBlockFinder
    {
        private BlockTable _blokTable;
        public string[] BlockNameToSearch { get; set; }
        public ObjectIdCollection BlockRefIds { get; set; }

        public DynamicBlockFinder(string[] blockNames)
        {
            BlockNameToSearch = blockNames;
        }

        public void GetLayoutsWithDynBlocks(Transaction trans, List<LayoutModel> layoutModels)
        {
            // получаем таблицу блоков и проходим по всем записям таблицы блоков
            _blokTable = (BlockTable) trans.GetObject(Active.Database.BlockTableId, OpenMode.ForRead);
            foreach (ObjectId btrId in _blokTable)
            {
                // получаем запись таблицы блоков и смотри анонимная ли она
                BlockTableRecord btr = (BlockTableRecord) trans.GetObject(btrId, OpenMode.ForRead);
                if (!btr.IsDynamicBlock || !BlockNameToSearch.Contains(btr.Name)) continue;
                // получаем все анонимные блоки динамического блока
                ObjectIdCollection anonymousIds = btr.GetAnonymousBlockIds();

                foreach (ObjectId anonymousBtrId in anonymousIds)
                {
                    // получаем анонимный блок
                    BlockTableRecord anonymousBtr =
                        (BlockTableRecord) trans.GetObject(anonymousBtrId, OpenMode.ForRead);
                    // получаем все вставки этого блока
                    BlockRefIds = anonymousBtr.GetBlockReferenceIds(true, true);

                    GetBlockInLayout(trans, layoutModels);
                }
            }
        }

        private void GetBlockInLayout(Transaction trans, List<LayoutModel> layoutModels)
        {
            foreach (ObjectId id in BlockRefIds)
            {
                var blockReference = (BlockReference) trans.GetObject(id, OpenMode.ForRead);
                ObjectId blockOwnerId = blockReference.OwnerId;

                foreach (var layout in layoutModels.OrderBy(x => x.Layout.TabOrder).ToList())
                {
                    var layoutId = layout.LayoutObjectOwnerId;
                    if (blockOwnerId == layoutId)
                    {
                        layout.BlocksObjectId = id;
                    }
                }
            }
        }
    }
}