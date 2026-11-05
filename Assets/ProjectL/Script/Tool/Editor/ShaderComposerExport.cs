using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    internal static class ShaderComposerExport
    {
        internal static void ApplyValues(Material material, ShaderComposerRecipe recipe)
        {
            foreach (ShaderComposerBinding binding in ShaderComposerGenerator.Bindings(recipe))
            {
                if (!material.HasProperty(binding.Name)) continue;
                switch (binding.Parameter.Kind)
                {
                    case ShaderComposerParameterKind.Color:
                        material.SetColor(binding.Name, binding.Value.color); break;
                    case ShaderComposerParameterKind.UV:
                    case ShaderComposerParameterKind.Vector2:
                    case ShaderComposerParameterKind.Vector4:
                        material.SetVector(binding.Name, binding.Value.vector); break;
                    case ShaderComposerParameterKind.Texture:
                        material.SetTexture(binding.Name, ResolveTexture(binding.Value.textureGuid)); break;
                    default:
                        material.SetFloat(binding.Name, binding.Value.number); break;
                }
            }
        }

        internal static Texture2D ResolveTexture(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
        }

        internal static Shader Write(ShaderComposerRecipe recipe, string assetPath, bool createMaterial)
        {
            ShaderComposerGenerator.Validate(recipe);
            assetPath = assetPath.Replace('\\', '/');
            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(Path.GetExtension(assetPath), ".shader", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Assets 폴더 아래의 .shader 경로를 선택해야 한다.");
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolute = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            string assetRoot = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!absolute.StartsWith(assetRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Assets 폴더 밖으로 내보낼 수 없다.");
            assetPath = "Assets/" + absolute.Substring(assetRoot.Length).Replace('\\', '/');
            string shaderName = recipe.shaderName.Trim();
            string conflict = FindNameConflict(shaderName, assetPath);
            ShaderComposerRecipe exportedRecipe = recipe;
            if (conflict != null)
            {
                if (File.Exists(absolute))
                    throw new InvalidOperationException("Shader 내부 이름 '" + shaderName + "'이(가) 다음 파일과 겹칩니다:\n" + conflict +
                        "\n\n파일명과 Shader 내부 이름은 별개입니다. 창 위쪽의 Shader 이름을 바꿔 다시 저장하세요.");
                int separator = shaderName.LastIndexOf('/');
                string prefix = separator < 0 ? string.Empty : shaderName.Substring(0, separator + 1);
                // 파일명에는 Shader 이름에서 허용하지 않는 문자가 있을 수 있다.
                string leaf = System.Text.RegularExpressions.Regex.Replace(Path.GetFileNameWithoutExtension(assetPath), @"[^\p{L}\p{N}_ .-]", "_");
                string candidate = prefix + leaf;
                shaderName = candidate;
                int suffix = 1;
                while (FindNameConflict(shaderName, assetPath) != null)
                    shaderName = candidate + "_" + suffix++;
                exportedRecipe = JsonUtility.FromJson<ShaderComposerRecipe>(JsonUtility.ToJson(recipe));
                exportedRecipe.shaderName = shaderName;
            }
            string source = ShaderComposerGenerator.Generate(exportedRecipe);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute, source, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            if (shader == null) throw new InvalidOperationException("Shader를 가져오지 못했다.");

            var names = new List<string>();
            var textures = new List<Texture>();
            foreach (ShaderComposerBinding binding in ShaderComposerGenerator.Bindings(exportedRecipe))
            {
                if (binding.Parameter.Kind != ShaderComposerParameterKind.Texture) continue;
                Texture texture = ResolveTexture(binding.Value.textureGuid);
                names.Add(binding.Name);
                textures.Add(texture);
            }
            if (names.Count > 0 && AssetImporter.GetAtPath(assetPath) is ShaderImporter importer)
            {
                importer.SetDefaultTextures(names.ToArray(), textures.ToArray());
                importer.SaveAndReimport();
                shader = AssetDatabase.LoadAssetAtPath<Shader>(assetPath);
            }
            if (createMaterial)
            {
                var material = new Material(shader) { name = Path.GetFileNameWithoutExtension(assetPath) };
                ApplyValues(material, exportedRecipe);
                string path = AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(assetPath, ".mat"));
                AssetDatabase.CreateAsset(material, path);
            }
            return shader;
        }

        private static string FindNameConflict(string shaderName, string targetPath)
        {
            // Shader.Find는 같은 이름의 임시 객체를 돌려줄 수 있으므로 실제 에셋을 먼저 확인한다.
            foreach (string guid in AssetDatabase.FindAssets("t:Shader"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(path, targetPath, StringComparison.OrdinalIgnoreCase)) continue;
                Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader != null && shader.name == shaderName) return path;
            }
            Shader existing = Shader.Find(shaderName);
            if (existing == null || !EditorUtility.IsPersistent(existing)) return null;
            string existingPath = AssetDatabase.GetAssetPath(existing);
            return !string.IsNullOrEmpty(existingPath) && !string.Equals(existingPath, targetPath, StringComparison.OrdinalIgnoreCase)
                ? existingPath : null;
        }
    }
}
