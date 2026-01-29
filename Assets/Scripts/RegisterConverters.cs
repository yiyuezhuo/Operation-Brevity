using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using TMPro;
using System.Linq;

using YYZ;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class RegisteredConverters
{
#if UNITY_EDITOR
    [InitializeOnLoadMethod]
#else
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
    public static void RegisterConverters()
    {
        Debug.Log("RegisterConverters");

        Register("Bool to DisplayStyle", (ref bool isShow) => (StyleEnum<DisplayStyle>)(isShow ? DisplayStyle.Flex : DisplayStyle.None));
        Register("Bool to DisplayStyle (Not)", (ref bool isShow) => (StyleEnum<DisplayStyle>)(isShow ? DisplayStyle.None : DisplayStyle.Flex));

    }

    static void Register<TSource, TDestination>(string name, TypeConverter<TSource, TDestination> converter)
    {
        var group = new ConverterGroup(name);
        group.AddConverter(converter);
        ConverterGroups.RegisterConverterGroup(group);
    }
}