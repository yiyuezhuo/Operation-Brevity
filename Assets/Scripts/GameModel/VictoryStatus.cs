using System.Collections.Generic;
using System;

namespace GameModel
{
    public partial class LossStats
    {
        public Side side;

        public int menLoss;
        public int gunLoss;
        public int vehicleLoss;

        public static float menLossCoef = 1f;
        public static float gunLossCoef = 10f;
        public static float vehicleLossCoef = 10f;

        public float lossVp => menLoss * menLossCoef + gunLoss * gunLossCoef + vehicleLoss * vehicleLossCoef;

        public static LossStats Capture(Side side)
        {
            var ret = new LossStats()
            {
                side = side
            };

            foreach(var unit in ((IOrderOfBattleNode)side).WalkChildren<Unit>())
            {
                int lossStrength = 0;
                if(unit.deployState == DeployState.Deployed)
                {
                    lossStrength = unit.parameter.Strength - unit.strength;
                }
                else if(unit.deployState == DeployState.Destroyed)
                {
                    lossStrength = unit.parameter.Strength;
                }
                
                if(unit.parameter.category == UnitParameter.personelCategory)
                {
                    ret.menLoss += lossStrength;
                }
                else if(unit.parameter.category == UnitParameter.gunCategory)
                {
                    ret.gunLoss += lossStrength;
                }
                else if(unit.parameter.category == UnitParameter.vehicleCategory)
                {
                    ret.vehicleLoss += lossStrength;
                }
            }

            return ret;
        }

    }

    public partial class VictoryStatus
    {
        public List<LossStats> lossStats = new();

        public static VictoryStatus Capture(GameState gameState)
        {
            var ret = new VictoryStatus();
            
            foreach(var side in gameState.sides)
            {
                ret.lossStats.Add(LossStats.Capture(side));
            }

            return ret;
        }

        public string Describe()
        {
            if (lossStats.Count < 2) return "Insufficient data";
            
            float sideAVp = lossStats[0].lossVp;
            float sideBVp = lossStats[1].lossVp;
            
            // Handle no losses case
            if (sideAVp == 0 && sideBVp == 0) return "Draw - No Casualties";
            
            // Calculate the loss ratio (smaller/larger)
            float smallerLoss = MathF.Min(sideAVp, sideBVp);
            float largerLoss = MathF.Max(sideAVp, sideBVp);
            float lossRatio = smallerLoss / largerLoss;
            
            // Determine winner
            string winner = sideAVp < sideBVp ? lossStats[0].side.name : lossStats[1].side.name;
            
            // Decision logic based on comment:
            // ratio < 20%: draw (actually ratio > 80% means difference < 20%)
            // < 100%: minor victory (winner's loss < loser's loss)
            // otherwise: major victory
            
            if (lossRatio > 0.8f)  // Difference less than 20%
            {
                return "Draw";
            }
            else if (lossRatio > 0.5f)  // Winner's loss is less than loser's loss
            {
                return $"{winner} Minor Victory";
            }
            else  // Winner has almost no losses compared to loser
            {
                return $"{winner} Major Victory";
            }
        }
    }
}