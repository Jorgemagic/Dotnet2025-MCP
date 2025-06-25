using Evergine.Common.Graphics;
using Evergine.Mathematics;
using EvergineMCPServer.Components;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace EvergineMCPServer.MCP
{
    [McpServerToolType]
    public static class Comunications
    {
        // Añade una de las ocho primitivas básicas
        [McpServerTool(Name = "AddBasicPrimitiveEntity"),
         Description("Añade únicamente una de las ocho primitivas básicas al escenario. Primitivas permitidas: Tetera → Teapot, Cubo → Cube, Pirámide → Pyramid, Toro → Torus, Esfera → Sphere, Cápsula → Capsule, Cilindro → Cylinder, Cono → Cone. Si necesitas otra forma distinta, usa AddAIGeneratedMesh.")]
        public static string AddBasicPrimitiveEntity(
            MyScene scene,

            [Description("Tipo de primitiva a crear (internamente se usa el nombre en inglés). El LLM puede recibir el nombre en español y lo convertirá al correspondiente en inglés: Tetera → Teapot, Cubo → Cube, Pirámide → Pyramid, Toro → Torus, Esfera → Sphere, Cápsula → Capsule, Cilindro → Cylinder, Cono → Cone. Ejemplo: 'Tetera' se envía como 'Teapot'.")]
            string entityType,

            [Description("Color de la entidad en formato hexadecimal RGBA. Ejemplo: '#FF0000FF' para rojo opaco. Por defecto '#FFFFFFFF' (blanco).")]
            string entityColor = "#FFFFFFFF",

            [Description("Coordenada X donde se colocará la entidad. Por defecto 0.")]
            float positionX = 0,

            [Description("Coordenada Y donde se colocará la entidad. Por defecto 0.")]
            float positionY = 0,

            [Description("Coordenada Z donde se colocará la entidad. Por defecto 0.")]
            float positionZ = 0,

            [Description("Rotación en radianes alrededor del eje X. Por defecto 0.")]
            float rotationX = 0,

            [Description("Rotación en radianes alrededor del eje Y. Por defecto 0.")]
            float rotationY = 0,

            [Description("Rotación en radianes alrededor del eje Z. Por defecto 0.")]
            float rotationZ = 0,

            [Description("Factor de escala en el eje X. Por defecto 1.")]
            float scaleX = 1,

            [Description("Factor de escala en el eje Y. Por defecto 1.")]
            float scaleY = 1,

            [Description("Factor de escala en el eje Z. Por defecto 1.")]
            float scaleZ = 1
        )
        {
            var api = scene.Managers.FindManager<APIInterface>();
            if (Enum.TryParse<APIInterface.PrimitiveTypes>(entityType, out var primitive))
            {
                var position = new Vector3(positionX, positionY, positionZ);
                var rotation = new Vector3(rotationX, rotationY, rotationZ);
                var scale = new Vector3(scaleX, scaleY, scaleZ);
                api.AddPrimitiveEntity(primitive, position, rotation, scale, new Color(entityColor));
            }
            return $"Entidad {entityType} añadida al escenario.";
        }

        // Añade un mesh 3D generado por IA
        [McpServerTool(Name = "AddAIGeneratedMesh"),
         Description("Genera y añade al escenario cualquier mesh 3D que no sea una de las ocho primitivas básicas. Recibe un texto descriptivo (prompt) con la forma y estilo deseados. No sirve para las primitivas de AddBasicPrimitiveEntity.")]
        public static async Task<string> AddAIGeneratedMesh(
            MyScene scene,

            [Description("Texto descriptivo (prompt) para generar la malla 3D. Ejemplo: 'un dragón estilizado con alas extensas'.")]
            string prompt,

            [Description("Coordenada X donde se colocará la entidad. Por defecto 0.")]
            float positionX = 0,

            [Description("Coordenada Y donde se colocará la entidad. Por defecto 0.")]
            float positionY = 0,

            [Description("Coordenada Z donde se colocará la entidad. Por defecto 0.")]
            float positionZ = 0,

            [Description("Rotación en radianes alrededor del eje X. Por defecto 0.")]
            float rotationX = 0,

            [Description("Rotación en radianes alrededor del eje Y. Por defecto 0.")]
            float rotationY = 0,

            [Description("Rotación en radianes alrededor del eje Z. Por defecto 0.")]
            float rotationZ = 0,

            [Description("Factor de escala en el eje X. Por defecto 1.")]
            float scaleX = 1,

            [Description("Factor de escala en el eje Y. Por defecto 1.")]
            float scaleY = 1,

            [Description("Factor de escala en el eje Z. Por defecto 1.")]
            float scaleZ = 1
        )
        {
            var api = scene.Managers.FindManager<APIInterface>();
            var position = new Vector3(positionX, positionY, positionZ);
            var rotation = new Vector3(rotationX, rotationY, rotationZ);
            var scale = new Vector3(scaleX, scaleY, scaleZ);
            await api.AddCustomEntity(prompt, position, rotation, scale);
            return $"Entidad generada con prompt '{prompt}' añadida al escenario.";
        }

        // Elimina todas las entidades
        [McpServerTool(Name = "ClearAllEntities"),
         Description("Elimina de la escena todas las entidades agregadas previamente, dejando el espacio limpio.")]
        public static string ClearAllEntities(MyScene scene)
        {
            var api = scene.Managers.FindManager<APIInterface>();
            api.ClearAllEntities();
            return "Todas las entidades han sido eliminadas de la escena.";
        }
    }
}
