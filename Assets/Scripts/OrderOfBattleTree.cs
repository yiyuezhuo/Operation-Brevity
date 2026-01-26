using System.Collections.Generic;
using GameModel;

public class OrderOfBattleTree : ITree<IOrderOfBattleNode, IOrderOfBattleNode> // CO2-like tree, side are top nodes, other are units
{
    public IOrderOfBattleNode GetParent(IOrderOfBattleNode node)
    {
        // return node switch
        // {
        //     Side side => null,
        //     Unit unit => unit.oobParentRef.Get() as IOrderOfBattleNode,
        //     _ => throw new System.Exception("Invalid node type")
        // };
        return node.parent;
    }

    public IEnumerable<IOrderOfBattleNode> GetChildren(IOrderOfBattleNode node)
    {
        // var oobChildrenRefs = node switch
        // {
        //     Side side => side.oobChildrenRefs,
        //     Unit unit => unit.oobChildrenRefs,
        //     _ => throw new System.Exception("Invalid node type")
        // };
        // foreach(var r in oobChildrenRefs)
        //     yield return r.Get() as IOrderOfBattleNode;
        return node.children;
    }

    public IOrderOfBattleNode GetData(IOrderOfBattleNode node)
    {
        // return node switch
        // {
        //     Side side => side.name,
        //     Unit unit => unit.name,
        //     _ => throw new System.Exception("Invalid node type")
        // };
        return node;
    }
}