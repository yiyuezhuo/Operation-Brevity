using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.Properties;
using UnityEngine;
using YYZ;
using YYZ.Unity;

namespace GameModel
{
    public partial class Unit
    {
        [CreateProperty]
        public string locationDesc
        {
            get
            {
                return deployState switch
                {
                    DeployState.NotDeployed => "(Not Deployed)",
                    DeployState.Deployed => $"({x}, {y})",
                    DeployState.Destroyed => "(Destroyed)",
                    _ => "Unknown"
                };
            }
        }

        public float stackPriority;

        public static int StackPriorityCompareTo(Unit u1, Unit u2)
        {
            return u1.stackPriority.CompareTo(u2.stackPriority);
        }

        [XmlIgnore]
        public CounterController view;

        [CreateProperty]
        public string oobDesc
        {
            get
            {
                var deployStr = deployState switch
                {
                    DeployState.NotDeployed => "(Not Deployed)",
                    DeployState.Deployed => "",
                    DeployState.Destroyed => "(Destroyed)",
                    _ => ""
                };
                return $"{name} {deployStr}";
            }
        }

        [CreateProperty]
        public string topText => CounterController.sizeStrMap[unitSize];

        [CreateProperty]
         public string bottomText => $"{GetPower():0}";
       //  public string bottomText => "10";

        [CreateProperty]
        public Color outerColorRectColor => CounterController.colorSchemaMap[country].outerRect;

        [CreateProperty]
        public Color innerColorRectColor => CounterController.colorSchemaMap[country].innerRect;

        [CreateProperty]
        public Color textColor => CounterController.colorSchemaMap[country].text;

        [CreateProperty]
        public Color iconColor => CounterController.colorSchemaMap[country].icon;

        [CreateProperty]
        public Texture2D iconTexture => StreamingAssetManagerEnumHelper<UnitType>.Instance.GetTexture2D(unitType);

        [CreateProperty]
        public float strengthProgress => (float)strength / parameter.Strength;

        [CreateProperty]
        public string strengthProgressDesc => $"{strength}/{parameter.Strength} {GetStrengthWord()}";

        [CreateProperty]
        public string readinessProgressDesc => $"{readiness:P}";

        [XmlIgnore]
        [CreateProperty]
        public List<IOrderOfBattleNode> parentsAndMeProp => parentsAndMe;
     }

    public partial class Side
    {
        [CreateProperty]
        public string oobDesc
        {
            get => name;
        }
    }

    public partial class Cell
    {
        [CreateProperty]
        public string desc => $"({x}, {y}) {terrain}";
    }

    public partial class LossStats
    {
        [CreateProperty]
        public float lossVpProp => lossVp; 
    }

    public partial class VictoryStatus
    {
        [CreateProperty]
        public string description => Describe();
    }
}

