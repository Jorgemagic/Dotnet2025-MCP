using Evergine.Bindings.Imgui;
using Evergine.Bindings.Imguizmo;
using Evergine.Common.Graphics;
using Evergine.Framework;
using Evergine.Mathematics;
using EvergineMCPServer.Components;
using EvergineMCPServer.TripoAI;
using System;

namespace EvergineMCPServer.UI
{
    public unsafe class UIBehavior : Behavior
    {
        [BindSceneManager]
        private APIInterface api = null;

        [BindService]
        private TripoAIService tripoAIService = null;

        private CreatePanel createPanel;

        private bool firstTime = true;

        protected override bool OnAttached()
        {
            var result = base.OnAttached();
                   
            this.createPanel = new CreatePanel(this.api, tripoAIService, this.Managers.EntityManager);

            return result;
        }

        protected override void Update(TimeSpan gameTime)
        {
            var io = ImguiNative.igGetIO_Nil();

            if (this.firstTime)
            {
                this.firstTime = false;
                Style.Apply();
            }

            ImguizmoNative.ImGuizmo_SetRect(0, 0, io->DisplaySize.X, io->DisplaySize.Y);

            var camera = this.Managers.RenderManager.ActiveCamera3D;
            Matrix4x4 view = camera.View;
            const int side = 128;
            ImguizmoNative.ImGuizmo_ViewManipulate_Float(&view.M11, 2, new Vector2(io->DisplaySize.X - side, 8), Vector2.One * side, (uint)Color.Transparent.ToInt());
            Matrix4x4.Invert(ref view, out Matrix4x4 iview);
            var translation = iview.Translation;
            var rotation = iview.Rotation;

            Vector3* r = &rotation;
            camera.Transform.LocalRotation = *r;

            Vector3* t = &translation;
            camera.Transform.LocalPosition = *t;

            // Panels            
            this.createPanel.Show(ref io);
        }
    }
}
