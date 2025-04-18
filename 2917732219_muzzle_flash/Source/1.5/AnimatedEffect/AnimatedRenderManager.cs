﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MuzzleFlash
{
    public class AnimatedRenderManager : BaseInstancedRenderManager<AnimatedRenderQueue>
    {
        private static AnimatedRenderManager _instance;
        public static AnimatedRenderManager Default
        {
            get
            {
                if (_instance == null) _instance = new AnimatedRenderManager();
                return _instance;
            }
        }

        public void AddInstance(int rendererID, Vector3 position, Quaternion rotattion, Vector3 scale, Color color = default, float frame = 0)
        {
            if (rendererID < -1 || rendererID >= _renderQueues.Count)
            {
                throw ExceptionRendering.InvalidRenderId(0, _renderQueues.Count, rendererID);
            }
            _renderQueues[rendererID].AddInstance(position, rotattion, scale, color, frame);
        }
    }
}
