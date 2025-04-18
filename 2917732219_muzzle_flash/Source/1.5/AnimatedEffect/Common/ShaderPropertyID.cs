﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace MuzzleFlash
{
    public static class ShaderPropertyID
    {
        public readonly static int frame = Shader.PropertyToID("_Frame");
        public readonly static int color = Shader.PropertyToID("_Color");
        public readonly static int splits = Shader.PropertyToID("_Splits");
        public readonly static int lightIntensity = Shader.PropertyToID("_LightIntensity");
    }
}
