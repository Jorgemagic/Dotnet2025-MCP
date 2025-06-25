using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Materials;
using Evergine.Framework.Managers;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Framework.Threading;
using Evergine.Mathematics;
using EvergineMCPServer.TripoAI;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace EvergineMCPServer.Components
{

    public class APIInterface : SceneManager
    {
        [BindService]
        protected AssetsService assetsService;

        [BindService]
        private TripoAIService tripoAIService = null;

        public event EventHandler<AIRequestEventArgs> AIRequest;

        private const string MODEL_FOLDER = "Models";

        public enum PrimitiveTypes
        {
            Teapot,
            Cube,
            Pyramid,
            Torus,
            Sphere,
            Capsule,
            Cylinder,
            Cone,
        };

        protected readonly string entityTag = "entityAdded";

        public void AddPrimitiveEntity(PrimitiveTypes primitive, Vector3 position, Vector3 rotation, Vector3 scale, Color color)
        {           
                MeshComponent mesh = null;
                switch (primitive)
                {
                    case PrimitiveTypes.Teapot:
                        mesh = new TeapotMesh();
                        break;
                    case PrimitiveTypes.Pyramid:
                        mesh = new PyramidMesh();
                        break;
                    case PrimitiveTypes.Torus:
                        mesh = new TorusMesh();
                        rotation += new Vector3(MathHelper.PiOver2, 0, 0);
                        break;
                    case PrimitiveTypes.Sphere:
                        mesh = new SphereMesh();
                        break;
                    case PrimitiveTypes.Capsule:
                        mesh = new CapsuleMesh();
                        break;
                    case PrimitiveTypes.Cylinder:
                        mesh = new CylinderMesh();
                        break;
                    case PrimitiveTypes.Cone:
                        mesh = new ConeMesh();
                        break;
                    default:
                    case PrimitiveTypes.Cube:
                        mesh = new CubeMesh();
                        break;
                }

                var material = this.assetsService.Load<Material>(DefaultResourcesIDs.DefaultMaterialID, true);
                StandardMaterial standard = new StandardMaterial(material);
                standard.BaseColor = color;

            EvergineForegroundTask.Run(() =>
            {
                Entity e = new Entity() { Tag = this.entityTag }
                    .AddComponent(new Transform3D() { Position = position, Rotation = rotation, Scale = scale })
                    .AddComponent(new MaterialComponent() { Material = standard.Material })
                    .AddComponent(mesh)
                    .AddComponent(new BoxCollider3D())
                    .AddComponent(new StaticBody3D())
                    .AddComponent(new MeshRenderer());

                this.Managers.EntityManager.Add(e);
            });
        }

        public async Task AddCustomEntity(string prompt, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            if (string.IsNullOrEmpty(prompt)) return;

            try
            {
                this.AIRequest?.Invoke(this, new AIRequestEventArgs("Starting the request ...", 0));

                // Request draft model
                var taskId = await this.tripoAIService.RequestADraftModel(prompt);

                if (string.IsNullOrEmpty(taskId))
                {
                    return;
                }

                // Waiting to task completed                
                string status = string.Empty;
                TripoResponse tripoResponse = default;
                while (status == string.Empty ||
                       status == "queued" ||
                       status == "running")
                {
                    await Task.Delay(500);
                    tripoResponse = await this.tripoAIService.GetTaskStatus(taskId);
                    status = tripoResponse.data.status;
                    this.AIRequest?.Invoke(this, new AIRequestEventArgs($"status: {status} progress:{tripoResponse.data.progress}", tripoResponse.data.progress));
                }

                if (status == "success")
                {
                    this.AIRequest?.Invoke(this, new AIRequestEventArgs("Download model ...", 100));

                    var modelURL = tripoResponse.data.output.pbr_model;
                    await EvergineForegroundTask.Run(async () => await this.DownloadModel(modelURL, position, rotation, scale));

                    this.AIRequest?.Invoke(this, new AIRequestEventArgs("Done!", 100));
                }
            }
            catch (Exception) { }
            finally
            { }
        }

        public void ClearAllEntities()
        {
            EvergineForegroundTask.Run(() =>
            {
                var entities = this.Managers.EntityManager.FindAllByTag(this.entityTag);
                foreach (var entity in entities)
                {
                    this.Managers.EntityManager.Remove(entity);
                }
            });
        }

        public async Task DownloadModel(string modelURL, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            var result = this.GetFilePathFromUrl(modelURL);
            var model = await this.DownloadModelFromURL(modelURL, result.filePath);
            var entity = model.InstantiateModelHierarchy(this.assetsService);

            var root = new Entity() { Tag = this.entityTag }
                            .AddComponent(new Transform3D() { Position = position, Rotation = rotation, Scale = scale });
            root.AddChild(entity);

            var boundingBox = model.BoundingBox.Value;
            boundingBox.Transform(entity.FindComponent<Transform3D>().WorldTransform);
            root.FindComponent<Transform3D>().Scale = Vector3.One * (1.0f / boundingBox.HalfExtent.Length());
            root.AddComponent(new BoxCollider3D()
            {
                Size = boundingBox.HalfExtent * 2,
                Offset = boundingBox.Center,
            });
            root.AddComponent(new StaticBody3D());

            this.Managers.EntityManager.Add(root);
        }

        private (string filePath, string fileName) GetFilePathFromUrl(string url, string modelName = null)
        {
            string fileNameWithExtension = Path.GetFileName(url);
            fileNameWithExtension = fileNameWithExtension.Substring(0, fileNameWithExtension.IndexOf("?"));
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileNameWithExtension);

            string fileName = modelName == null ? fileNameWithoutExtension : modelName;
            string extension = Path.GetExtension(fileNameWithExtension);

            string filePath = Path.Combine(MODEL_FOLDER, $"{fileName}{extension}");

            int index = 0;
            while (File.Exists(filePath))
            {
                index++;
                filePath = Path.Combine(MODEL_FOLDER, $"{fileName}_{index}{extension}");
            }

            return (filePath, $"{fileName}_{index}");
        }

        private async Task<Evergine.Framework.Graphics.Model> DownloadModelFromURL(string url, string filePath)
        {
            Evergine.Framework.Graphics.Model result = null;
            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(url))
                {
                    response.EnsureSuccessStatusCode();

                    // Save file to disc
                    await this.DownloadFileTaskAsync(client, new Uri(url), filePath);

                    // Read file
                    using (var fileStream = new FileStream(filePath, FileMode.Open))
                    {
                        result = await Evergine.Runtimes.GLB.GLBRuntime.Instance.Read(fileStream);
                    }
                }
            }

            return result;
        }

        private async Task DownloadFileTaskAsync(HttpClient client, Uri uri, string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var s = await client.GetStreamAsync(uri))
            {
                using (var fs = new FileStream(filePath, FileMode.CreateNew))
                {
                    await s.CopyToAsync(fs);
                }
            }
        }
    }
}
