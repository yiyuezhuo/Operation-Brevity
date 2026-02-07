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
    
    [SerializeField]
    float moveDurationSeconds = 0.2f;

    Vector3 moveFrom;
    Vector3 moveTo;
    float moveElapsed;
    bool moveActive;
    bool hasInitializedPosition;

    Color topTextBaseColor;
    Color bottomTextBaseColor;
    Color outerColorBaseColor;
    Color innerColorBaseColor;
    Color iconBaseColor;

    float fadeElapsed;
    float fadeDuration;
    bool fadeActive;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(fadeActive)
        {
            UpdateFadeOut();
            return;
        }

        Sync();
        UpdateMoveAnimation();
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

            topTextBaseColor = topText.color;
            bottomTextBaseColor = botttomText.color;
            outerColorBaseColor = outerColorRect.color;
            innerColorBaseColor = innerColorRect.color;
            iconBaseColor = unitIcon.color;
        }
    }

    public void SetTargetPosition(Vector3 position)
    {
        if(!hasInitializedPosition)
        {
            transform.position = position;
            hasInitializedPosition = true;
            moveActive = false;
            moveElapsed = 0f;
            moveFrom = position;
            moveTo = position;
            return;
        }

        if(Vector3.Distance(transform.position, position) <= 0.001f)
        {
            moveActive = false;
            moveElapsed = 0f;
            moveFrom = position;
            moveTo = position;
            transform.position = position;
            return;
        }

        moveFrom = transform.position;
        moveTo = position;
        moveElapsed = 0f;
        moveActive = true;
    }

    public void SetImmediatePosition(Vector3 position)
    {
        transform.position = position;
        hasInitializedPosition = true;
        moveActive = false;
        moveElapsed = 0f;
        moveFrom = position;
        moveTo = position;
    }

    public void BeginFadeOutAndDestroy(float durationSeconds)
    {
        if(fadeActive)
            return;

        fadeActive = true;
        fadeDuration = Mathf.Max(0.01f, durationSeconds);
        fadeElapsed = 0f;
        unit = null;
    }

    void UpdateMoveAnimation()
    {
        if(!moveActive)
            return;

        var duration = Mathf.Max(0.01f, moveDurationSeconds);
        moveElapsed += Time.deltaTime;
        var t = Mathf.Clamp01(moveElapsed / duration);
        var eased = Mathf.SmoothStep(0f, 1f, t);
        transform.position = Vector3.Lerp(moveFrom, moveTo, eased);

        if(t >= 1f)
        {
            moveActive = false;
            transform.position = moveTo;
        }
    }

    void UpdateFadeOut()
    {
        fadeElapsed += Time.deltaTime;
        var t = Mathf.Clamp01(fadeElapsed / fadeDuration);
        var alpha = 1f - t;

        topText.color = WithAlpha(topTextBaseColor, alpha);
        botttomText.color = WithAlpha(bottomTextBaseColor, alpha);
        outerColorRect.color = WithAlpha(outerColorBaseColor, alpha);
        innerColorRect.color = WithAlpha(innerColorBaseColor, alpha);
        unitIcon.color = WithAlpha(iconBaseColor, alpha);

        if(t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
