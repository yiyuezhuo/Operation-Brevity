using UnityEngine;
using TMPro;
using GameModel;
using System.Collections.Generic;
using YYZ.Unity;
using UnityEngine.Rendering;

public class CounterController : MonoBehaviour
{
    public TMP_Text topText;
    public TMP_Text botttomText;
    public SpriteRenderer outerColorRect;
    public SpriteRenderer innerColorRect;
    public SpriteRenderer unitIcon;
    public SortingGroup sortingGroup;

    public Unit unit; // model

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Sync();
    }

    public class ColorSchema // TOAW-like Color Schema
    {
        public Color outerRect;
        public Color innerRect;
        public Color text;
        public Color icon;
    }

    public static Dictionary<Country, ColorSchema> colorSchemaMap = new()
    {
        [Country.Britain] = new()
        {
            outerRect = new Color32(130, 110, 41, 255),
            // outerRect = Color.green,
            // innerRect = new Color32(255, 255, 255, 255),
            // innerRect = Color.red,
            innerRect = Color.white,
            text = Color.white,
            // text = Color.purple,
            // icon = new(132, 8, 8)
            icon = new Color32(132, 8, 8, 255)
        },
        [Country.Germany] = new()
        {
            // outerRect = new(126, 126, 126),
            outerRect = new Color32(126, 126, 126, 255),
            innerRect = Color.white,
            // innerRect = new(255, 255, 255),
            // text = new(0, 0, 0),
            // icon = new(0, 0, 0)
            text = Color.black,
            icon = Color.black
        },
        [Country.Italy] = new()
        {
            // outerRect = new(255, 255, 255),
            // outerRect = Color.white,
            outerRect = new Color32(126, 126, 126, 255),
            // innerRect = new(129, 7, 0),
            innerRect = new Color32(129, 7, 0, 255),
            // text = new(0, 0, 0),
            // icon = new(0, 0, 0)
            text = Color.black,
            // icon = Color.black
            icon = Color.white
        }
    };

    public static Dictionary<UnitSize, string> sizeStrMap = new()
    {
        { UnitSize.NotSpecified, "" },
        { UnitSize.Division, "XX" },
        { UnitSize.Brigade, "X" },
        { UnitSize.Regiment, "III" },
        { UnitSize.Battalion, "II" },
        { UnitSize.Company, "I" },
        { UnitSize.Platton, "···" },
    };

    public void Sync()
    {
        if(unit != null)
        {
            topText.text = sizeStrMap[unit.unitSize];
            // botttomText.text = "10";
            botttomText.text = $"{unit.GetPower():0}";

            var colorSchema = colorSchemaMap[unit.country];
            topText.color = botttomText.color = colorSchema.text;
            
            outerColorRect.color = colorSchema.outerRect;
            
            innerColorRect.color = colorSchema.innerRect;
            unitIcon.color = colorSchema.icon;

            unitIcon.sprite = StreamingAssetManagerEnumHelper<UnitType>.Instance.GetSprite(unit.unitType);
        }
    }
}
