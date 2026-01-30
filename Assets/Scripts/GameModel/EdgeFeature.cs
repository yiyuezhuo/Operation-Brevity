using System.Xml.Serialization;


namespace GameModel
{
    // public class EdgeState
    // {
    //     public bool hasPrimaryRoad;
    //     public bool hasSecondaryRoad;
    //     public bool moveBlocked; // Escarpment
    //     public bool isFort; //  Fort is unidirectional
    // }

    public enum EdgeFeatureType
    {
        PrimaryRoad,
        SecondaryRoad,
        Escarpment,
    }


    public class EdgeFeature // extra feature like primary road, secondary road, terrain block
    {
        [XmlAttribute]
        public int x1; // source

        [XmlAttribute]
        public int y1;

        [XmlAttribute]
        public int x2; // destination

        [XmlAttribute]
        public int y2;

        [XmlAttribute]
        public bool primaryRoad; // symmetric

        [XmlAttribute]
        public bool secondaryRoad; // symmetric

        [XmlAttribute]
        public bool escarpment; // not-symmetric

        public static int CompareTo(EdgeFeature e1, EdgeFeature e2)
        {
            var r1 = e1.x1.CompareTo(e2.x1);
            if (r1 != 0) return r1;
            var r2 = e1.y1.CompareTo(e2.y1);
            if (r2 != 0) return r2;
            var r3 = e1.x2.CompareTo(e2.x2);
            if (r3 != 0) return r3;
            return e1.y2.CompareTo(e2.y2);
        }

        public bool IsDefault() => !primaryRoad && !secondaryRoad && !escarpment;

        public bool Get(EdgeFeatureType featureType)
        {
            if(featureType == EdgeFeatureType.PrimaryRoad)
            {
                return primaryRoad;
            }
            else if(featureType == EdgeFeatureType.SecondaryRoad)
            {
                return secondaryRoad;
            }
            else if(featureType == EdgeFeatureType.Escarpment)
            {
                return escarpment;
            }
            return false;
        }

        // public bool IsPassable() => !escarpment;
    }
}