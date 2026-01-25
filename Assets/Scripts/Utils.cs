using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

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

        var diff = length - childList.Count;
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
                GameObject.Destroy(childList[i]);
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
}