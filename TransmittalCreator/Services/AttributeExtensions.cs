using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace TransmittalCreator.Services
{
    public static class AttributeExtensions
    {
        public static IEnumerable<AttributeReference> GetAttributes(this AttributeCollection attribs)
        {
            foreach (ObjectId id in attribs)
            {
                var attRef = (AttributeReference) id.GetObject(OpenMode.ForRead, false, false);
                    if(attRef.Visible)
                        yield return attRef;
            }
        }

        public static Dictionary<string, string> GetAttributesValues(this BlockReference br)
        {
            return br.AttributeCollection
                .GetAttributes()
                .ToDictionary(att => att.Tag, att => att.TextString);
        }


        public static void SetAttributesValues(this BlockReference br, Dictionary<string, string> atts)
        {
            foreach (AttributeReference attRef in br.AttributeCollection.GetAttributes())
            {
                if (atts.ContainsKey(attRef.Tag))
                {
                    attRef.UpgradeOpen();
                    attRef.TextString = atts[attRef.Tag];
                }
            }
        }
    }
}
