using Evergine.Framework;
using EvergineMCPServer.Components;
using EvergineMCPServer.ImGui;
using EvergineMCPServer.UI;

namespace EvergineMCPServer
{
    public class MyScene : Scene
    {
        public override void RegisterManagers()
        {
            base.RegisterManagers();

            this.Managers.AddManager(new global::Evergine.Bullet.BulletPhysicManager3D());
            this.Managers.AddManager(new CustomImGuiManager()
            {
                ImGuizmoEnabled = true,
                CustomFonts = ["Fonts/Roboto.ttf"],
            });
            this.Managers.AddManager(new APIInterface());
        }

        protected override void CreateScene()
        {
            Entity ui = new Entity()
               .AddComponent(new UIBehavior());
            this.Managers.EntityManager.Add(ui);

            Entity manipulation = new Entity() { Tag = "Picking" }
                .AddComponent(new Manipulation());
            this.Managers.EntityManager.Add(manipulation);            
        }
    }
}


