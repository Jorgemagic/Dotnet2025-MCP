using Evergine.Bindings.Imgui;
using Evergine.Bindings.Imguizmo;
using Evergine.Bindings.Imnodes;
using Evergine.Bindings.Implot;
using Evergine.Common.Graphics;
using Evergine.Common.Input.Keyboard;
using Evergine.Common.Input.Mouse;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Managers;
using Evergine.Mathematics;
using EvergineMCPServer.Components;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Buffer = Evergine.Common.Graphics.Buffer;

namespace EvergineMCPServer.ImGui
{
    public unsafe class CustomImGuiManager : SceneManager
    {
        [BindService]
        private GraphicsContext graphicsContext = null;

        [BindSceneManager]
        private RenderManager renderManager = null;

        private Buffer[] vertexBuffers;
        private Buffer indexBuffer;
        private InputLayouts vertexLayouts;
        private Buffer constantBuffer;
        private Texture fontTexture;
        private SamplerState sampler;
        private GraphicsPipelineState pipelineState;
        private ResourceLayout layout;
        private ResourceSet resourceSet;
        private ImGuiIO* io;

        private IntPtr imguiContext;
        private IntPtr implotContext;
        private IntPtr imnodesContext;

        private int windowWidth;
        private int windowHeight;
        private Vector2 scaleFactor;

        private IntPtr fontAtlasID;
        private Surface surface;
        private FrameBuffer framebuffer;

        private int lastAssignedID = 100;
        private Matrix4x4 mvp;

        private Camera camera;
        private Shader vertexShader;
        private Shader pixelShader;

        private CameraBehavior cameraBehavior;
        private Manipulation manipulation;

        /// <summary>
        /// Gets or sets a value indicating whether the Imguizmo extension is enabled.
        /// </summary>
        public bool ImGuizmoEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Implot extension is enabled.
        /// </summary>
        public bool ImPlotEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Imnodes extension is enabled.
        /// </summary>
        public bool ImNodesEnabled { get; set; }

        /// <summary>
        /// Gets or sets an string array indicating paths to custom fonts to load.
        /// </summary>
        public string[] CustomFonts { get; set; }

        private bool focus;
        public bool ImGuiMouseFocus
        {
            get => this.focus;

            set
            {
                if (this.focus != value)
                {
                    this.focus = value;
                    this.cameraBehavior.IsEnabled = !this.focus;
                    this.manipulation.PickingEnabled = !this.focus;
                }
            }
        }

        private struct ResourceSetInfo
        {
            public readonly IntPtr ImGuiBinding;
            public readonly ResourceSet ResourceSet;

            public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet)
            {
                this.ImGuiBinding = imGuiBinding;
                this.ResourceSet = resourceSet;
            }
        }

        private readonly Dictionary<Texture, ResourceSetInfo> resourceByTexture = new Dictionary<Texture, ResourceSetInfo>();
        private readonly Dictionary<IntPtr, ResourceSetInfo> resourceById = new Dictionary<IntPtr, ResourceSetInfo>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ImGuiManager"/> class.
        /// </summary>
        public CustomImGuiManager()
        {
            this.scaleFactor = Vector2.One;
            this.fontAtlasID = (IntPtr)1;
        }

        /// <inheritdoc/>
        protected override bool OnAttached()
        {
            var result = base.OnAttached();

            if (!Application.Current.IsEditor)
            {
                this.InitializeResources();
            }

            return result;
        }

        /// <inheritdoc/>
        protected override void OnActivated()
        {
            base.OnActivated();
            this.camera = this.renderManager.ActiveCamera3D;
            this.cameraBehavior = this.camera.Owner.FindComponent<CameraBehavior>();
            this.manipulation = this.Managers.EntityManager.FindFirstComponentOfType<Manipulation>();

            if (!Application.Current.IsEditor)
            {
                this.framebuffer = this.camera.Display.FrameBuffer;

                if (this.framebuffer == null)
                {
                    throw new Exception("ImGuiManager: Display does not have an attached frame buffer");
                }

                this.surface = this.camera.Display.Surface;
                this.camera.Display.DisplaySizeChanged += this.Display_DisplaySizeChanged;
                this.camera.Display.DisplayFrameBufferChanged += this.Display_DisplayFrameBufferChanged;
                this.camera.DrawContext.OnPostRender += this.DrawContext_OnPostRender;

                this.ConfigurePipeline();
            }
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            if (!Application.Current.IsEditor)
            {
                this.BeginFrame(TimeSpan.Zero);
            }

            base.Start();
        }

        /// <inheritdoc/>
        protected override void OnDeactivated()
        {
            this.camera.Display.DisplaySizeChanged -= this.Display_DisplaySizeChanged;
            this.camera.Display.DisplayFrameBufferChanged -= this.Display_DisplayFrameBufferChanged;
            if (this.camera.DrawContext != null)
            {
                this.camera.DrawContext.OnPostRender -= this.DrawContext_OnPostRender;
            }

            this.camera = null;

            // Register input events
            var mouseDispatcher = this.surface.MouseDispatcher;
            mouseDispatcher.MouseButtonDown -= this.MouseDispatcher_MouseButtonDown;
            mouseDispatcher.MouseButtonUp -= this.MouseDispatcher_MouseButtonUp;
            mouseDispatcher.MouseMove -= this.MouseDispatcher_MouseMove;
            mouseDispatcher.MouseScroll -= this.MouseDispatcher_MouseScroll;

            var keyboardDispatcher = this.surface.KeyboardDispatcher;
            keyboardDispatcher.KeyDown -= this.KeyboardDispatcher_KeyDown;
            keyboardDispatcher.KeyUp -= this.KeyboardDispatcher_KeyUp;
            keyboardDispatcher.KeyChar -= this.KeyboardDispatcher_KeyChar;

            base.OnDeactivated();
        }

        private void Display_DisplayFrameBufferChanged(object sender, EventArgs e)
        {
            this.framebuffer = this.camera.Display.FrameBuffer;
        }

        private void Display_DisplaySizeChanged(object sender, Evergine.Common.Helpers.SizeEventArgs e)
        {
            this.windowWidth = (int)e.Width;
            this.windowHeight = (int)e.Height;
        }

        private void BeginFrame(TimeSpan gameTime)
        {
            this.io->DisplaySize = new Vector2(
                            this.windowWidth / this.scaleFactor.X,
                            this.windowHeight / this.scaleFactor.Y);

            this.io->DisplayFramebufferScale = this.scaleFactor;
            this.io->DeltaTime = (float)gameTime.TotalSeconds;

            // Read keyboard modifiers input
            var keyboardDispatcher = this.surface.KeyboardDispatcher;
            this.io->KeyCtrl = keyboardDispatcher.IsKeyDown(Keys.LeftControl) ? (byte)1 : (byte)0;
            this.io->KeyShift = keyboardDispatcher.IsKeyDown(Keys.LeftShift) ? (byte)1 : (byte)0;
            this.io->KeyAlt = keyboardDispatcher.IsKeyDown(Keys.LeftAlt) ? (byte)1 : (byte)0;

            // Set orthographics projection matrix
            this.mvp = Matrix4x4.CreateOrthographicOffCenter(
                0f,
                this.io->DisplaySize.X,
                this.io->DisplaySize.Y,
                0.0f,
                -1.0f,
                1.0f,
                this.graphicsContext.Capabilities.ClipDepth == ClipDepth.ZeroToOne);

            if (this.framebuffer.RequireFlipProjection)
            {
                this.mvp.M22 *= -1;
            }

            ImguiNative.igNewFrame();

            if (this.ImGuizmoEnabled)
            {
                ImguizmoNative.ImGuizmo_BeginFrame();
            }
        }

        private unsafe void InitializeResources()
        {
            // Create imgui context
            this.imguiContext = ImguiNative.igCreateContext((ImFontAtlas*)null);
            ImguiNative.igSetCurrentContext(this.imguiContext);

            // Create imguizmo context
            if (this.ImGuizmoEnabled)
            {
                ImguizmoNative.ImGuizmo_SetImGuiContext(this.imguiContext);
            }

            // Create implot context
            if (this.ImPlotEnabled)
            {
                ImplotNative.ImPlot_SetImGuiContext(this.imguiContext);
                this.implotContext = ImplotNative.ImPlot_CreateContext();
            }

            // Create imnodes context
            if (this.ImNodesEnabled)
            {
                ImnodesNative.imnodes_SetImGuiContext(this.imguiContext);
                this.imnodesContext = ImnodesNative.imnodes_CreateContext();
            }

            this.io = ImguiNative.igGetIO_Nil();

            // Add custom fonts to ImGUI.
            /*if (this.CustomFonts?.Length > 0)
            {
                foreach (var font in this.CustomFonts)
                {
                    if (this.assetsDirectory.Exists(font))
                    {
                        using (var stream = this.assetsDirectory.Open(font))
                        using (var memoryStream = new MemoryStream())
                        {
                            stream.CopyTo(memoryStream);
                            byte[] bytes = memoryStream.ToArray();
                            fixed (byte* pointer = bytes)
                            {
                                this.io->Fonts->AddFontFromMemoryTTF(pointer, 14, 14);
                            }
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("Couldn't find " + font + " in " + System.IO.Directory.GetCurrentDirectory());
                    }
                }
            }
            else*/
            {
                this.io->Fonts->AddFontDefault(null);
            }

            // Configure error behavior
            this.io->ConfigErrorRecoveryEnableAssert = 0;
            this.io->ConfigErrorRecoveryEnableTooltip = 0;

            // Compile shaders.
            var vsCode = this.NativeAPICompiler(ShaderStages.Vertex);
            var psCode = this.NativeAPICompiler(ShaderStages.Pixel);

            // Create native resources
            var vertexShaderDescription = new ShaderDescription(ShaderStages.Vertex, "VS", vsCode);
            var pixelShaderDescription = new ShaderDescription(ShaderStages.Pixel, "PS", psCode);

            this.vertexShader = this.graphicsContext.Factory.CreateShader(ref vertexShaderDescription);
            this.pixelShader = this.graphicsContext.Factory.CreateShader(ref pixelShaderDescription);

            var vertexBufferDescription = new BufferDescription(
                8192,
                BufferFlags.VertexBuffer,
                ResourceUsage.Dynamic,
                ResourceCpuAccess.Write);

            this.vertexBuffers = new Buffer[1];
            this.vertexBuffers[0] = this.graphicsContext.Factory.CreateBuffer(ref vertexBufferDescription);

            var indexBufferDescription = new BufferDescription(
                2048,
                BufferFlags.IndexBuffer,
                ResourceUsage.Dynamic,
                ResourceCpuAccess.Write);

            this.indexBuffer = this.graphicsContext.Factory.CreateBuffer(ref indexBufferDescription);

            this.vertexLayouts = new InputLayouts()
                  .Add(new LayoutDescription()
                              .Add(new ElementDescription(ElementFormat.Float2, ElementSemanticType.Position))
                              .Add(new ElementDescription(ElementFormat.Float2, ElementSemanticType.TexCoord))
                              .Add(new ElementDescription(ElementFormat.UByte4Normalized, ElementSemanticType.Color)));

            var layoutDescription = new ResourceLayoutDescription(
                    new LayoutElementDescription(0, ResourceType.ConstantBuffer, ShaderStages.Vertex),
                    new LayoutElementDescription(0, ResourceType.Texture, ShaderStages.Pixel),
                    new LayoutElementDescription(0, ResourceType.Sampler, ShaderStages.Pixel));

            this.layout = this.graphicsContext.Factory.CreateResourceLayout(ref layoutDescription);

            var constantBufferDescription = new BufferDescription((uint)Unsafe.SizeOf<Matrix4x4>(), BufferFlags.ConstantBuffer, ResourceUsage.Default);
            this.constantBuffer = this.graphicsContext.Factory.CreateBuffer(ref constantBufferDescription);

            // Create Font Texture
            int width;
            int height;
            int bytesPerPixel;
            byte* pixels = null;
            this.io->Fonts->GetTexDataAsRGBA32(&pixels, &width, &height, &bytesPerPixel);

            this.io->Fonts->SetTexID(this.fontAtlasID);

            var fontTextureDescription = new TextureDescription()
            {
                Type = TextureType.Texture2D,
                Width = (uint)width,
                Height = (uint)height,
                Format = PixelFormat.R8G8B8A8_UNorm,
                Usage = ResourceUsage.Default,
                Depth = 1,
                Faces = 1,
                ArraySize = 1,
                MipLevels = 1,
                SampleCount = TextureSampleCount.None,
                CpuAccess = ResourceCpuAccess.Write,
                Flags = TextureFlags.ShaderResource,
            };

            this.fontTexture = this.graphicsContext.Factory.CreateTexture(ref fontTextureDescription);
            this.graphicsContext.UpdateTextureData(this.fontTexture, (IntPtr)pixels, (uint)(bytesPerPixel * width * height), 0);

            SamplerStateDescription samplerDescription = new SamplerStateDescription()
            {
                Filter = TextureFilter.MinLinear_MagLinear_MipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                MipLODBias = 0f,
                MaxAnisotropy = 1,
                ComparisonFunc = ComparisonFunction.Never,
                MinLOD = 0f,
                MaxLOD = 0f,
            };

            this.sampler = this.graphicsContext.Factory.CreateSamplerState(ref samplerDescription);

            var resourceSetDescription = new ResourceSetDescription(this.layout, this.constantBuffer, this.fontTexture, this.sampler);
            this.resourceSet = this.graphicsContext.Factory.CreateResourceSet(ref resourceSetDescription);

            this.io->Fonts->ClearTexData();
        }

        private unsafe void ConfigurePipeline()
        {
            // Create pipeline
            var blendState = BlendStates.AlphaBlend;
            blendState.AlphaToCoverageEnable = false;
            blendState.RenderTarget0.BlendEnable = true;
            blendState.RenderTarget0.SourceBlendColor = Blend.SourceAlpha;
            blendState.RenderTarget0.DestinationBlendColor = Blend.InverseSourceAlpha;
            blendState.RenderTarget0.BlendOperationColor = BlendOperation.Add;
            blendState.RenderTarget0.SourceBlendAlpha = Blend.SourceAlpha;
            blendState.RenderTarget0.DestinationBlendAlpha = Blend.Zero;
            blendState.RenderTarget0.BlendOperationAlpha = BlendOperation.Add;

            var rasterizerState = RasterizerStates.None;
            rasterizerState.FillMode = FillMode.Solid;
            rasterizerState.CullMode = CullMode.None;
            rasterizerState.ScissorEnable = true;
            rasterizerState.DepthClipEnable = true;

            var pipelineDescription = new GraphicsPipelineDescription()
            {
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                InputLayouts = this.vertexLayouts,
                ResourceLayouts = new[] { this.layout },
                Shaders = new GraphicsShaderStateDescription()
                {
                    VertexShader = this.vertexShader,
                    PixelShader = this.pixelShader,
                },
                RenderStates = new RenderStateDescription()
                {
                    RasterizerState = rasterizerState,
                    BlendState = blendState,
                    DepthStencilState = DepthStencilStates.None,
                },
                Outputs = this.framebuffer.OutputDescription,
            };

            this.windowWidth = (int)this.framebuffer.Width;
            this.windowHeight = (int)this.framebuffer.Height;

            this.pipelineState = this.graphicsContext.Factory.CreateGraphicsPipeline(ref pipelineDescription);

            // Register input events
            var mouseDispatcher = this.surface.MouseDispatcher;
            mouseDispatcher.MouseButtonDown += this.MouseDispatcher_MouseButtonDown;
            mouseDispatcher.MouseButtonUp += this.MouseDispatcher_MouseButtonUp;
            mouseDispatcher.MouseMove += this.MouseDispatcher_MouseMove;
            mouseDispatcher.MouseScroll += this.MouseDispatcher_MouseScroll;

            var keyboardDispatcher = this.surface.KeyboardDispatcher;
            keyboardDispatcher.KeyDown += this.KeyboardDispatcher_KeyDown;
            keyboardDispatcher.KeyUp += this.KeyboardDispatcher_KeyUp;
            keyboardDispatcher.KeyChar += this.KeyboardDispatcher_KeyChar;
        }

        private void KeyboardDispatcher_KeyChar(object sender, KeyCharEventArgs e)
        {
            this.io->AddInputCharacter(e.Character);
        }

        private void KeyboardDispatcher_KeyUp(object sender, KeyEventArgs e)
        {
            if (this.TryMapKey(e.Key, out ImGuiKey imguiKey))
            {
                this.io->AddKeyEvent(imguiKey, false);
            }
        }

        private void KeyboardDispatcher_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.TryMapKey(e.Key, out ImGuiKey imguiKey))
            {
                this.io->AddKeyEvent(imguiKey, true);
            }
        }

        private void MouseDispatcher_MouseScroll(object sender, MouseScrollEventArgs e)
        {
            this.io->AddMouseWheelEvent(e.Delta.X, e.Delta.Y);
        }

        private void MouseDispatcher_MouseMove(object sender, MouseEventArgs e)
        {
            this.io->AddMousePosEvent(e.Position.X / this.surface.DPIDensity, e.Position.Y / this.surface.DPIDensity);
        }

        private void MouseDispatcher_MouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    this.io->AddMouseButtonEvent((int)ImGuiMouseButton.Left, false);
                    break;
                case MouseButtons.Right:
                    this.io->AddMouseButtonEvent((int)ImGuiMouseButton.Right, false);
                    break;
                case MouseButtons.Middle:
                    this.io->AddMouseButtonEvent((int)ImGuiMouseButton.Middle, false);
                    break;
                default:
                    break;
            }
        }

        private void MouseDispatcher_MouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left:
                    this.io->AddMouseButtonEvent((int)ImGuiMouseButton.Left, true);
                    break;
                case MouseButtons.Right:
                    this.io->AddMouseButtonEvent((int)ImGuiMouseButton.Right, true);
                    break;
                case MouseButtons.Middle:
                    this.io->AddMouseButtonEvent((int)ImGuiMouseButton.Middle, true);
                    break;
            }
        }

        private bool TryMapKey(Keys key, out ImGuiKey result)
        {
            ImGuiKey KeyToImGuiKeyShortcut(Keys keyToConvert, Keys startKey1, ImGuiKey startKey2)
            {
                int changeFromStart1 = (int)keyToConvert - (int)startKey1;
                return startKey2 + changeFromStart1;
            }

            if (key >= Keys.F1 && key <= Keys.F24)
            {
                result = KeyToImGuiKeyShortcut(key, Keys.F1, ImGuiKey.F1);
            }
            else if (key >= Keys.D0 && key <= Keys.D9)
            {
                result = KeyToImGuiKeyShortcut(key, Keys.D0, ImGuiKey.Keypad0);
            }
            else if (key >= Keys.A && key <= Keys.Z)
            {
                result = KeyToImGuiKeyShortcut(key, Keys.A, ImGuiKey.A);
            }
            else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                result = KeyToImGuiKeyShortcut(key, Keys.NumPad0, ImGuiKey._0);
            }
            else if (key == Keys.LeftShift || key == Keys.RightShift)
            {
                result = ImGuiKey.ImGuiMod_Shift;
            }
            else if (key == Keys.LeftControl || key == Keys.RightControl)
            {
                result = ImGuiKey.ImGuiMod_Ctrl;
            }
            else if (key == Keys.LeftAlt || key == Keys.RightAlt)
            {
                result = ImGuiKey.ImGuiMod_Alt;
            }
            else if (key == Keys.LeftWindows || key == Keys.RightWindows)
            {
                result = ImGuiKey.ImGuiMod_Super;
            }
            else
            {
                switch (key)
                {
                    case Keys.Up: result = ImGuiKey.UpArrow; break;
                    case Keys.Down: result = ImGuiKey.DownArrow; break;
                    case Keys.Left: result = ImGuiKey.LeftArrow; break;
                    case Keys.Right: result = ImGuiKey.RightArrow; break;
                    case Keys.Enter: result = ImGuiKey.Enter; break;
                    case Keys.Escape: result = ImGuiKey.Escape; break;
                    case Keys.Space: result = ImGuiKey.Space; break;
                    case Keys.Tab: result = ImGuiKey.Tab; break;
                    case Keys.Back: result = ImGuiKey.Backspace; break;
                    case Keys.Insert: result = ImGuiKey.Insert; break;
                    case Keys.Delete: result = ImGuiKey.Delete; break;
                    case Keys.PageUp: result = ImGuiKey.PageUp; break;
                    case Keys.PageDown: result = ImGuiKey.PageDown; break;
                    case Keys.Home: result = ImGuiKey.Home; break;
                    case Keys.End: result = ImGuiKey.End; break;
                    case Keys.CapsLock: result = ImGuiKey.CapsLock; break;
                    case Keys.Scroll: result = ImGuiKey.ScrollLock; break;
                    case Keys.PrintScreen: result = ImGuiKey.PrintScreen; break;
                    case Keys.Pause: result = ImGuiKey.Pause; break;
                    case Keys.NumLock: result = ImGuiKey.NumLock; break;
                    case Keys.Divide: result = ImGuiKey.KeypadDivide; break;
                    case Keys.Multiply: result = ImGuiKey.KeypadMultiply; break;
                    case Keys.Subtract: result = ImGuiKey.KeypadSubtract; break;
                    case Keys.Add: result = ImGuiKey.KeypadAdd; break;
                    case Keys.Decimal: result = ImGuiKey.KeypadDecimal; break;
                    case Keys.OemTilde: result = ImGuiKey.GraveAccent; break;
                    case Keys.OemMinus: result = ImGuiKey.Minus; break;
                    case Keys.OemPlus: result = ImGuiKey.Equal; break;
                    case Keys.OemSemicolon: result = ImGuiKey.Semicolon; break;
                    case Keys.OemQuotes: result = ImGuiKey.Apostrophe; break;
                    case Keys.OemComma: result = ImGuiKey.Comma; break;
                    case Keys.OemPeriod: result = ImGuiKey.Period; break;
                    case Keys.OemBackslash: result = ImGuiKey.Backslash; break;
                    default: result = ImGuiKey.None; break;
                }
            }

            return result != ImGuiKey.None;
        }

        private byte[] NativeAPICompiler(ShaderStages stage)
        {
            string shaderCode = string.Empty;
            string entryPoint = stage == ShaderStages.Vertex ? "VS" : "PS";
            byte[] byteCode = null;

            switch (this.graphicsContext.BackendType)
            {
                case GraphicsBackend.OpenGL:
                    shaderCode = stage == ShaderStages.Vertex ? Shaders.GLSLVertexShader : Shaders.GLSLPixelShader;
                    break;
                case GraphicsBackend.Metal:
                    shaderCode = stage == ShaderStages.Vertex ? Shaders.MSLVertexShader : Shaders.MSLPixelShader;
                    break;
                case GraphicsBackend.Vulkan:
                    string code = stage == ShaderStages.Vertex ? Shaders.SPIRVVertexShader : Shaders.SPIRVPixelShader;
                    byteCode = Convert.FromBase64String(code);
                    break;
                case GraphicsBackend.DirectX12:
                case GraphicsBackend.DirectX11:
                default:
                    shaderCode = stage == ShaderStages.Vertex ? Shaders.HLSLVertexShader : Shaders.HLSLPixelShader;
                    break;
            }

            if (!string.IsNullOrEmpty(shaderCode))
            {
                CompilerParameters parameters = new CompilerParameters()
                {
                    CompilationMode = CompilationMode.None,
                    Profile = GraphicsProfile.Level_10_0,
                };

                var result = this.graphicsContext.ShaderCompile(shaderCode, entryPoint, stage, parameters);
                if (!result.HasErrors)
                {
                    byteCode = result.ByteCode;
                }
            }

            return byteCode;
        }

        /// <summary>
        /// Gets or creates a new ImGui texture binding.
        /// </summary>
        /// <param name="texture">The texture to bind.</param>
        /// <returns>The binding pointer.</returns>
        public IntPtr CreateImGuiBinding(Texture texture)
        {
            if (!this.resourceByTexture.TryGetValue(texture, out ResourceSetInfo info))
            {
                var resourceSetDescriptionnew = new ResourceSetDescription(this.layout, this.constantBuffer, texture, this.sampler);
                var newResourceSet = this.graphicsContext.Factory.CreateResourceSet(ref resourceSetDescriptionnew);
                info = new ResourceSetInfo(this.GetNextImGuiBindingID(), newResourceSet);

                this.resourceByTexture.Add(texture, info);
                this.resourceById.Add(info.ImGuiBinding, info);
            }

            return info.ImGuiBinding;
        }

        /// <summary>
        /// Removes an ImGui texture binding.
        /// </summary>
        /// <param name="texture">The texture to remove from the binding list.</param>
        public void RemoveImGuiBinding(Texture texture)
        {
            if (this.resourceByTexture.TryGetValue(texture, out ResourceSetInfo info))
            {
                this.resourceByTexture.Remove(texture);
                this.resourceById.Remove(info.ImGuiBinding);
                info.ResourceSet.Dispose();
            }
        }

        private IntPtr GetNextImGuiBindingID()
        {
            int newID = this.lastAssignedID++;
            return (IntPtr)newID;
        }

        private ResourceSet GetImageResourceSet(IntPtr textureId)
        {
            if (this.resourceById.TryGetValue(textureId, out ResourceSetInfo rsi))
            {
                return rsi.ResourceSet;
            }

            return null;
        }

        /// <summary>
        /// ImGui Renderer using low-level API.
        /// </summary>
        /// <param name="drawContext">The graphics draw context.</param>
        /// <param name="commandBuffer">The commandBuffer to draw.</param>
        private unsafe void DrawContext_OnPostRender(Evergine.Framework.Graphics.DrawContext drawContext, CommandBuffer commandBuffer)
        {
            ImguiNative.igRender();

            this.ImGuiMouseFocus = Convert.ToBoolean(io->WantCaptureMouse);

            uint vertexOffsetInVertices = 0;
            uint indexOffsetInElements = 0;

            ImDrawData* drawData = ImguiNative.igGetDrawData();

            if (drawData->CmdListsCount > 0)
            {
                // Resize index and vertex buffers.
                int vertexBufferSize = drawData->TotalVtxCount * sizeof(ImDrawVert);
                if (vertexBufferSize > this.vertexBuffers[0].Description.SizeInBytes)
                {
                    this.vertexBuffers[0].Dispose();
                    uint nextSize = (uint)MathHelper.NextPowerOfTwo(vertexBufferSize);
                    var vertexBufferDescription = new BufferDescription(
                        nextSize,
                        BufferFlags.VertexBuffer,
                        ResourceUsage.Dynamic,
                        ResourceCpuAccess.Write);

                    this.vertexBuffers[0] = this.graphicsContext.Factory.CreateBuffer(ref vertexBufferDescription) ;
                }

                int indexBufferSize = drawData->TotalIdxCount * sizeof(ushort);
                if (indexBufferSize > this.indexBuffer.Description.SizeInBytes)
                {
                    this.indexBuffer.Dispose();
                    uint nextSize = (uint)MathHelper.NextPowerOfTwo(indexBufferSize);
                    var indexBufferDescription = new BufferDescription(
                        nextSize,
                        BufferFlags.IndexBuffer,
                        ResourceUsage.Dynamic,
                        ResourceCpuAccess.Write);

                    this.indexBuffer = this.graphicsContext.Factory.CreateBuffer(ref indexBufferDescription);
                }

                // Update index and vertex buffers.
                var vResource = this.graphicsContext.MapMemory(this.vertexBuffers[0], MapMode.Write);
                var iResource = this.graphicsContext.MapMemory(this.indexBuffer, MapMode.Write);

                for (int i = 0; i < drawData->CmdListsCount; i++)
                {
                    ImVector<IntPtr> cmdList = new ImVector<IntPtr>(drawData->CmdLists);
                    ImDrawList* cmdListPtr = (ImDrawList*)cmdList[i];

                    // Copy vertex
                    var vOffset = vertexOffsetInVertices * (uint)sizeof(ImDrawVert);
                    Unsafe.CopyBlock((void*)((long)vResource.Data + vOffset), (void*)cmdListPtr->VtxBuffer.Data, (uint)(cmdListPtr->VtxBuffer.Size * sizeof(ImDrawVert)));

                    // Copy index
                    var iOffset = indexOffsetInElements * sizeof(ushort);
                    Unsafe.CopyBlock((void*)((long)iResource.Data + iOffset), (void*)cmdListPtr->IdxBuffer.Data, (uint)(cmdListPtr->IdxBuffer.Size * sizeof(ushort)));

                    vertexOffsetInVertices += (uint)cmdListPtr->VtxBuffer.Size;
                    indexOffsetInElements += (uint)cmdListPtr->IdxBuffer.Size;
                }

                this.graphicsContext.UnmapMemory(this.vertexBuffers[0]);
                this.graphicsContext.UnmapMemory(this.indexBuffer);

                commandBuffer.BeginDebugMarker("ImGUI");
                commandBuffer.UpdateBufferData(this.constantBuffer, ref this.mvp);

                RenderPassDescription renderPassDescription = new RenderPassDescription(this.framebuffer, ClearValue.None);
                commandBuffer.BeginRenderPass(ref renderPassDescription);

                // Bind resources
                commandBuffer.SetGraphicsPipelineState(this.pipelineState);
                commandBuffer.SetVertexBuffers(this.vertexBuffers);
                commandBuffer.SetIndexBuffer(this.indexBuffer, IndexFormat.UInt16);

                drawData->ScaleClipRects(this.io->DisplayFramebufferScale);

                // Render command lists
                uint vtx_offset = 0;
                uint idx_offset = 0;

                for (int n = 0; n < drawData->CmdListsCount; n++)
                {
                    ImVector<IntPtr> cmdList = new ImVector<IntPtr>(drawData->CmdLists);
                    ImDrawList* cmdListPtr = (ImDrawList*)cmdList[n];

                    for (int i = 0; i < cmdListPtr->CmdBuffer.Size; i++)
                    {
                        ImDrawCmd* cmd = (ImDrawCmd*)((long)cmdListPtr->CmdBuffer.Data + (i * sizeof(ImDrawCmd)));

                        if (cmd->TextureId != IntPtr.Zero)
                        {
                            if (cmd->TextureId == this.fontAtlasID)
                            {
                                commandBuffer.SetResourceSet(this.resourceSet);
                            }
                            else
                            {
                                commandBuffer.SetResourceSet(this.GetImageResourceSet(cmd->TextureId));
                            }
                        }

                        var scissors = new Rectangle[1]
                        {
                            new Rectangle(
                            (int)cmd->ClipRect.X,
                            (int)cmd->ClipRect.Y,
                            (int)(cmd->ClipRect.Z - cmd->ClipRect.X),
                            (int)(cmd->ClipRect.W - cmd->ClipRect.Y)),
                        };

                        // Fix scissors (Flip Y) in OpenGL devices...
                        if (this.graphicsContext.Capabilities.FlipProjectionRequired)
                        {
                            ref var scissorRectangle = ref scissors[0];
                            scissorRectangle.Y = (int)this.camera.Display.Height - scissorRectangle.Y - scissorRectangle.Height;
                        }

                        commandBuffer.SetScissorRectangles(scissors);

                        commandBuffer.DrawIndexedInstanced(cmd->ElemCount, 1, idx_offset, vtx_offset, 0);

                        idx_offset += cmd->ElemCount;
                    }

                    vtx_offset += (uint)cmdListPtr->VtxBuffer.Size;
                }

                commandBuffer.EndDebugMarker();
                commandBuffer.EndRenderPass();
            }

            this.BeginFrame(drawContext.ElapsedTime);
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            base.Destroy();

            foreach (var rsi in this.resourceById)
            {
                rsi.Value.ResourceSet.Dispose();
            }

            this.resourceByTexture.Clear();
            this.resourceById.Clear();

            if (this.ImPlotEnabled)
            {
                ImplotNative.ImPlot_SetImGuiContext(IntPtr.Zero);
                ImplotNative.ImPlot_SetCurrentContext(IntPtr.Zero);
                ImplotNative.ImPlot_DestroyContext(this.implotContext);
            }

            if (this.ImNodesEnabled)
            {
                ImnodesNative.imnodes_SetCurrentContext(IntPtr.Zero);
                ImnodesNative.imnodes_SetImGuiContext(IntPtr.Zero);
                ImnodesNative.imnodes_DestroyContext(this.imnodesContext);
            }

            ImguiNative.igDestroyContext(this.imguiContext);

            this.vertexBuffers[0].Dispose();
            this.vertexBuffers = null;
            this.indexBuffer.Dispose();
            this.constantBuffer.Dispose();
            this.layout.Dispose();
            this.resourceSet.Dispose();
            this.vertexLayouts = null;
            this.vertexShader.Dispose();
            this.vertexShader = null;
            this.pixelShader.Dispose();
            this.pixelShader = null;
        }
    }
}