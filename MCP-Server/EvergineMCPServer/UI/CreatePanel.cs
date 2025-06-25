using Evergine.Bindings.Imgui;
using Evergine.Framework;
using Evergine.Framework.Managers;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Evergine.UI;
using EvergineMCPServer.Components;
using EvergineMCPServer.TripoAI;
using System.Text;
using System.Threading.Tasks;

namespace EvergineMCPServer.UI
{
    public class CreatePanel
    {
        private const string MODEL_FOLDER = "Models";
        private byte[] promptTextBuffer = new byte[1024];

        private int progress = 0;
        private string msg = string.Empty;

        private TripoAIService tripoAIService;
        private EntityManager entityManager;
        private AssetsService assetsService;

        public bool OpenWindow = true;

        private readonly int windowsWidth = 260;
        private readonly int windowsHeight = 700;
        private readonly Vector2 buttonSize = new Vector2(230, 25);

        private int primitive;
        private Vector3 position = Vector3.Zero;
        private Vector3 rotationDegree = Vector3.Zero;
        private Vector3 rotationRadians = Vector3.Zero;
        private Vector3 scale = Vector3.One;
        private Vector3 color = Vector3.One;

        private APIInterface api;        

        public CreatePanel(APIInterface api, TripoAIService tripoAIService, EntityManager entityManager)
        {
            this.api = api;
            this.tripoAIService = tripoAIService;
            this.entityManager = entityManager;
            this.assetsService = Application.Current.Container.Resolve<AssetsService>();

            this.api.AIRequest -= Api_AIRequest;
            this.api.AIRequest += Api_AIRequest;
        }

        private void Api_AIRequest(object sender, AIRequestEventArgs e)
        {
            this.msg = e.Message;
            this.progress = e.Progress;
        }

        public unsafe void Show(ref ImGuiIO* io)
        {
            if (this.OpenWindow)
            {
                ImguiNative.igSetNextWindowPos(new Vector2(10, 10), ImGuiCond.Appearing, new Vector2(0, 0));
                ImguiNative.igSetNextWindowSize(new Vector2(this.windowsWidth, this.windowsHeight), ImGuiCond.Appearing);
                ImguiNative.igBegin("CreatePanel", this.OpenWindow.Pointer(), ImGuiWindowFlags.NoResize);

                int i = this.primitive;
                ImguiNative.igRadioButton_IntPtr("Teapot", &i, 0);
                ImguiNative.igSameLine(130, 0);
                ImguiNative.igRadioButton_IntPtr("Cube", &i, 1);

                ImguiNative.igRadioButton_IntPtr("Pyramid", &i, 2);
                ImguiNative.igSameLine(130, 0);
                ImguiNative.igRadioButton_IntPtr("Torus", &i, 3);

                ImguiNative.igRadioButton_IntPtr("Sphere", &i, 4);
                ImguiNative.igSameLine(130, 0);
                ImguiNative.igRadioButton_IntPtr("Capsule", &i, 5);

                ImguiNative.igRadioButton_IntPtr("Cylinder", &i, 6);
                ImguiNative.igSameLine(130, 0);
                ImguiNative.igRadioButton_IntPtr("Cone", &i, 7);

                this.primitive = i;

                ImguiNative.igSeparator();

                Vector3 c = this.color;
                ImguiNative.igColorPicker3("Color", &c, ImGuiColorEditFlags.None);
                this.color = c;

                ImguiNative.igSeparator();

                Vector3 p = this.position;
                ImguiNative.igInputFloat3("Position", &p, null, ImGuiInputTextFlags.None);
                this.position = p;

                Vector3 r = this.rotationDegree;
                ImguiNative.igInputFloat3("Rotation", &r, null, ImGuiInputTextFlags.None);
                this.rotationDegree = r;

                Vector3 s = this.scale;
                ImguiNative.igInputFloat3("Scale", &s, null, ImGuiInputTextFlags.None);
                this.scale = s;

                ImguiNative.igSeparator();                

                if (ImguiNative.igButton("Add new entity", this.buttonSize))
                {
                    this.rotationRadians.X = MathHelper.ToRadians(this.rotationDegree.X);
                    this.rotationRadians.Y = MathHelper.ToRadians(this.rotationDegree.Y);
                    this.rotationRadians.Z = MathHelper.ToRadians(this.rotationDegree.Z);
                    this.api.AddPrimitiveEntity((APIInterface.PrimitiveTypes)this.primitive, this.position, this.rotationRadians, this.scale,  new Evergine.Common.Graphics.Color(this.color.X, this.color.Y, this.color.Z));
                }

                if (ImguiNative.igButton("Clear all entities", this.buttonSize))
                {
                    this.api.ClearAllEntities();
                }

                ImguiNative.igSeparator();

                fixed (byte* promptBuffer = promptTextBuffer)
                {
                    ImguiNative.igText("Prompt");
                    ImguiNative.igInputTextMultiline(
                        "##prompt",
                        promptBuffer,
                        (uint)promptTextBuffer.Length,
                        new Vector2(this.buttonSize.X, 35),
                        ImGuiInputTextFlags.EnterReturnsTrue,
                        null,
                        null);                    

                    ImguiNative.igProgressBar(this.progress / 100.0f, this.buttonSize, this.msg);                    
                    if (ImguiNative.igButton("Create", this.buttonSize))
                    {
                        string prompt = Encoding.UTF8.GetString(promptBuffer, promptTextBuffer.Length);
                        var p_index = prompt.IndexOf('\0');
                        if (p_index >= 0)
                        {
                            prompt = prompt.Substring(0, p_index);
                        }                        

                        this.RequestDraftModel(prompt);
                    }
                }


                ImguiNative.igEnd();
            }
        }

        private void RequestDraftModel(string prompt)
        {
            Task.Run(() => this.api.AddCustomEntity(prompt, Vector3.Zero, Vector3.Zero, Vector3.One));      
        }       
    }
}
