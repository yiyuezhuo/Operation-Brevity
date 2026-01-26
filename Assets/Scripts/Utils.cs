using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.Properties;

public static class Utils
{
    public static Transform CreateDynamicTransform(Transform parentTransform, string name)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parentTransform);
        return obj.transform;
    }

    public static void DestroyChildrensFor(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
        }
    }

    public static void SyncTransformViewerLength(Transform containerTransform, int length, GameObject prefab)
    {
        List<GameObject> childList = new List<GameObject>();

        for (int i = 0; i < containerTransform.childCount; i++)
        {
            childList.Add(containerTransform.GetChild(i).gameObject);
        }

        var childListCount = childList.Count;
        var diff = length - childListCount;
        if (diff > 0)
        {
            for (int i = 0; i < diff; i++)
            {
                GameObject.Instantiate(prefab, containerTransform);
            }
        }
        else if (diff < 0)
        {
            for (int i = 0; i < -diff; i++)
            {
                // GameObject.Destroy(childList[i]);
                GameObject.Destroy(childList[childListCount - 1 - i]);
            }
        }
    }

    public static void BindItemsSourceRecursive(VisualElement root)
    {
        foreach (var listView in root.Query<BaseListView>().ToList())
        {
            listView.SetBinding("itemsSource", new DataBinding());
        }
    }

    public static bool TryResolveCurrentValueForBinding<T>(VisualElement el, out T ret) where T: class
    {
        var ctx = el.GetHierarchicalDataSourceContext();
        
        if(ctx.dataSourcePath.Length == 0)
        {
            ret = ctx.dataSource as T;
            return ret != null;
        }

        return PropertyContainer.TryGetValue(ctx.dataSource, ctx.dataSourcePath, out ret);
    }
}