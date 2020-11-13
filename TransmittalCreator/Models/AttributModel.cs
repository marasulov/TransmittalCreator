using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransmittalCreator.Models
{
    public class AttributModel
    {


        /// <summary>
        /// Номер листа
        /// </summary>
        public string ObjectNameEn { get; set; }
        public string ObjectNameRu { get; set; }

        /// <summary>
        /// Номер листа
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// Document Number
        /// </summary>
        public string Nomination { get; set; }
        /// <summary>
        /// Document Name in English
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Document Title (English)
        /// </summary>
        public string TrItem { get; set; }

        /// <summary>
        /// documnet name in rus
        /// </summary>
        public string TrDocNumber { get; set; }
        /// <summary>
        /// Document Title (Rus)
        /// </summary>
        public string TrDocTitleEn { get; set; }
        public string TrDocTitleRu { get; set; }

        public AttributModel(string position, string nomination, string comment, string trItem, string trDocNumber, 
            string trDocTitleEn, string trDocTitleRu)
        {
            this.Position = position;
            this.Nomination = nomination;
            this.Comment = comment;
            this.TrItem = trItem;
            this.TrDocNumber = trDocNumber;
            this.TrDocTitleEn = trDocTitleEn;
            this.TrDocTitleRu = trDocTitleRu;
        }

        public AttributModel(BlockModel objectNameEn, BlockModel objectNameRu,BlockModel position, BlockModel nomination, BlockModel comment, BlockModel trItem, BlockModel trDocNumber,
            BlockModel trDocTitleEn, BlockModel trDocTitleRu)
        {
            this.ObjectNameEn = objectNameEn.AttributName;
            this.ObjectNameRu = objectNameRu.AttributName;
            this.Position = position.AttributName;
            this.Nomination = nomination.AttributName;
            if(comment!=null)
            this.Comment = comment.AttributName;
            this.TrItem = trItem.AttributName;
            this.TrDocNumber = trDocNumber.AttributName;
            this.TrDocTitleEn = trDocTitleEn.AttributName;
            this.TrDocTitleRu = trDocTitleRu.AttributName;
        }
    }
}
