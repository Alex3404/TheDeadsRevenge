using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts
{
    [System.Serializable]
    public class Upgradable
    {
        public string name = "";
        public float defaultValue, maxValue, Increment, startPrice, priceMultiplier = 0;
    }
}
