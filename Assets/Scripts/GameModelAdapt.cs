using System.Security.Cryptography;
using System.Xml.Serialization;
using Unity.Properties;

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
     }

    public partial class Side
    {
        [CreateProperty]
        public string oobDesc
        {
            get => name;
        }
    }
}

