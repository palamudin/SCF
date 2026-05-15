using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Unity {

    [CreateAssetMenu(fileName = "ScatterRule", menuName = "Atlas/Unity/Scatter Rule", order = 1)]
    public class ScatterRuleAsset : ScriptableObject {

        //[Header("Splatmap")]
        [Tooltip("The target splatmap on which you want to scatter the objects.")]
        public int target = 1;
        [Tooltip("The minimum value that the mask should be.")]
        [Range(0, 1)]
        public float maskFactorMin = 0.2f;
        [Tooltip("The maximum value that the mask should be.")]
        [Range(0, 1)]
        public float maskFactorMax = 1f;

        //[Header("Objects")]
        [Tooltip("Objects won't be placed on objects with this tag name.")]
        public string blockTagLayer;
        [Tooltip("When enabled objects will only be placed on colliders with tagName.")]
        public bool invertTagLayer;

        //[Header("Slope")]
        [Tooltip("Minimum slope value.")]
        [Range(0, 90)]
        public float slopeMin = 0f;
        [Tooltip("Maximum slope value.")]
        [Range(0, 90)]
        public float slopeMax = 30f;

        //[Header("Height")]
        [Tooltip("Minimum height value.")]
        [Range(0, 10000)]
        public float heightMin = 0f;
        [Tooltip("Maximum height value.")]
        [Range(0, 10000)]
        public float heightMax = 10000f;

        //[Header("Placement")]
        [Range(0.1f, 200)]
        [Tooltip("Spacing of the first layer of scattering in Unity units.")]
        public float spacing = 10;
        [Tooltip("The seed used for generating random values.")]
        public int seed = 0;
        [Tooltip("When enabled the scattered objects will cull each other.")]
        public bool selfCulling = false;

        public List<ScatterPass> scatterStack;
        public List<DetailPass> detailStack;

        [System.Serializable]
        public class ScatterPass {

            [HideInInspector]
            public int[] treePrototypeIndexes;
            [HideInInspector]
            public bool folded = false;

            public GameObject[] prefabs;
            public int mode = 0;

            [Tooltip("Radius where other trees will be removed to counter overlap.")]
            [Range(0, 200)]
            public float cullRadius = 1;
            [Tooltip("Radius where details will be removed to counter overlap.")]
            [Range(0, 200)]
            public float detailCullRadius = 1;
            [Tooltip("Radius where child elements will spawn.")]
            [Range(0, 200)]
            public float spawnRadius = 5;
            [Tooltip("How many children are spawned within spawnRadius.")]
            [Range(0, 20)]
            public int childCount = 3;

            [Tooltip("Minimum value of the scale multiplier.")]
            [Range(0.1f, 10)]
            public float scaleMultiplierMin = 1;
            [Tooltip("Maximum value of the scale multiplier.")]
            [Range(0.1f, 10)]
            public float scaleMultiplierMax = 1;
            [Tooltip("Multiplies the scale based on the mask used to determine if the object will be placed or not. lower values will make the object smaller as the mask fades out")]
            [Range(0.1f, 10)]
            public float maskScaleMultiplier = 1;

            [Tooltip("Minimum value of the height scale.")]
            [Range(0.1f, 10)]
            public float heightScaleMin = 1;
            [Tooltip("Maximum value of the height scale.")]
            [Range(0.1f, 10)]
            public float heightScaleMax = 1;

            [Tooltip("Minimum value of the width scale.")]
            [Range(0.1f, 10)]
            public float widthScaleMin = 1;
            [Tooltip("Maximum value of the width scale.")]
            [Range(0.1f, 10)]
            public float widthScaleMax = 1;

            [Tooltip("Scale multiplier based on distance from parent.")]
            [Range(0.1f, 10)]
            public float distanceScaleMultiplier = 1;

            [Tooltip("How much the prefab will align with the terrain slope.")]
            [Range(0, 1)]
            public float alignment = 0f;

            [Tooltip("Minimum value of the height offset.")]
            [Range(-5, 5)]
            public float heightOffsetMin = 0f;
            [Tooltip("Maximum value of the width offset.")]
            [Range(-5, 5)]
            public float heightOffsetMax = 0f;

            public bool inspectorInitialized = false;

        }

        [System.Serializable]
        public class DetailPass {

            [HideInInspector]
            public int[] detailPrototypeIndexes;
            [HideInInspector]
            public bool folded = false;

            public Texture2D[] textures;
            public GameObject[] prefabs;

            public bool useMesh = false;
            [Tooltip("Detail scattering density.")]
            [Range(1, 100)]
            public int density = 10;
            [Tooltip("Minimum height.")]
            [Range(0.1f, 10)]
            public float minHeight = 1;
            [Tooltip("Maximum height.")]
            [Range(0.1f, 10)]
            public float maxHeight = 1;
            [Tooltip("Minimum width.")]
            [Range(0.1f, 10)]
            public float minWidth = 1;
            [Tooltip("Maximum width.")]
            [Range(0.1f, 10)]
            public float maxWidth = 1;
            [Tooltip("The distance from terrain holes where no detail will be added.")]
            [Range(0.1f, 10f)]
            public float holeEdgePadding = 1;
            [Tooltip("Allows you to select a render mode for details.")]
            public DetailRenderMode renderMode = DetailRenderMode.GrassBillboard;
            [Tooltip("Enables instancing on the detail objects.")]
            public bool useInstancing = true;

            public bool inspectorInitialized = false;

        }

    }

}