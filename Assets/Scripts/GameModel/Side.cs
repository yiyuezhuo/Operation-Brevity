using System.Collections.Generic;
using System.Linq;

namespace GameModel
{
    public partial class Side : IObjectIdLabeled, IOrderOfBattleNode, IHasObjRef
    {
        public string objectId{get; set;}

        // public string name{get; set;}
        public string name;

        public IEnumerable<IObjectIdLabeled> GetSubObjects()
        {
            yield break;
        }

        public List<ObjRef> oobChildrenRefs = new();

        public IOrderOfBattleNode parent => null;
        public IEnumerable<IOrderOfBattleNode> children => oobChildrenRefs.Select(x => x.Get() as IOrderOfBattleNode);

        public override string ToString()
        {
            return $"Side({name})";
        }

        public IEnumerable<ObjRef> IterateObjRefs()
        {
            foreach(var objRef in oobChildrenRefs)
                yield return objRef;
        }

    }
}