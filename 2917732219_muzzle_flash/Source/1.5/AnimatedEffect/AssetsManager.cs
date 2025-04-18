using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using Verse;

namespace MuzzleFlash
{
    public class AssetsManager
    {
        //private const string EmbeddedAssetBundlePath = "MuzzleFlash.vocore";
        private const string EmbeddedAssetBundlePath = "MuzzleFlash.shaders";
        private static AssetsManager _instance;
        public static AssetsManager Default
        {
            get
            {
                if (_instance == null) _instance = new AssetsManager();
                return _instance;
            }
        }

        private readonly Dictionary<string, Shader> _shaders = new Dictionary<string, Shader>();
        private readonly List<Material> _materials = new List<Material>();

        private Shader _animatedInstanced;
        private AssetBundle _assets;
        private bool _initialized = false;

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            //string shaderBundleName = EmbeddedAssetBundlePath;
            string shaderBundleName = EmbeddedAssetBundlePath + GetPlatformPostfix();
            Log.Message($"Loading asset bundle {shaderBundleName}");
            using (Stream stream = currentAssembly.GetManifestResourceStream(shaderBundleName))
            {
                LoadAssetBundle(AssetBundle.LoadFromStream(stream));
            }
        }

        public void LoadAssetBundle(AssetBundle assets)
        {
            UnloadAssetBundle();
            _shaders.Clear();
            this._assets = assets;
            foreach (var shader in assets.LoadAllAssets<Shader>())
            {
                Log.Message($"Loaded shader {shader.name}");
                _shaders.Add(shader.name, shader);
            }
        }

        public void UnloadAssetBundle()
        {
            if (_assets == null) return;
            _shaders.Clear();
            _materials.Clear();
            _assets.Unload(true);
            _assets = null;
        }

        public Shader GetShader(string key)
        {
            if (!_initialized) Initialize();
            if (!_shaders.TryGetValue(key, out Shader shader))
            {
                return null;
            }
            return shader;
        }

        public IEnumerable<string> GetShaderNames()
        {
            foreach (var shader in _shaders)
            {
                yield return shader.Value.name;
            }
        }

        public Material GetMaterial(Shader shader, Texture2D texture, Vector4 splits, float lightIntensity = 1f)
        {
            if (shader == null) throw ExceptionRendering.ShaderNotFound(" Null shader");

            for (int i = 0; i < _materials.Count; i++)
            {
                if (_materials[i].shader != shader) continue;
                if (_materials[i].mainTexture != texture) continue;
                if (_materials[i].GetVector(ShaderPropertyID.splits) != splits) continue;
                if (_materials[i].GetFloat(ShaderPropertyID.lightIntensity) != lightIntensity) continue;
                return _materials[i];
            }

            Material result = UtilsMaterial.CreateMaterial(shader, true);
            result.mainTexture = texture;
            result.SetVector(ShaderPropertyID.splits, splits);
            result.SetFloat(ShaderPropertyID.lightIntensity, lightIntensity);
            return result;
        }


        public Shader ShaderAnimatedAdditiveInstanced
        {
            get
            {
                if (_animatedInstanced == null) _animatedInstanced = GetShader("Unlit/AnimatedAdditiveInstanced");
                if (_animatedInstanced == null) ExceptionRendering.ShaderNotFound("Unlit/AnimatedAdditiveInstanced");
                // if (_animatedInstanced == null) _animatedInstanced = GetShader("Unlit/AnimatedInstanced");
                // if (_animatedInstanced == null) ExceptionRendering.ShaderNotFound("Unlit/AnimatedInstanced");
                return _animatedInstanced;
            }
        }

        private string GetPlatformPostfix()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "-windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "-macos";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    return "-linux";
                default:
                    return "-unknown";
            }
        }
    }
}
