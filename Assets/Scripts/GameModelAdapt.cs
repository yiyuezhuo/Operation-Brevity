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
     }

    public partial class Side
    {
    }
}

