using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Unity {

    [CreateAssetMenu(fileName = "StampAsset", menuName = "Atlas/Unity/Stamp Asset", order = 0)]
    public class StampAsset : ScriptableObject {

        public Texture2D height;
        public Texture2D color;
        [Tooltip("Main mask of the stamp.")]
        public Texture2D mask;
        public Texture2D splat1;
        public Texture2D splat2;
        [Tooltip("Used exclusively for road blending, this mask is usually a copy of the main mask but narrower.")]
        public Texture2D roadMask;

    }

}