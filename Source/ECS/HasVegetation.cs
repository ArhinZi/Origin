﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Origin.Source.ECS
{
    public struct HasVegetation
    {
        public string VegetationID;
        private byte _volume;

        public byte Volume
        {
            get => _volume;
            set => _volume = (byte)(value > 10 ? 10 : value);
        }
    }
}