using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace GrasshopperComponents.Components.Crowd;

public sealed class OpenFlowTemplateComponent : IndGhComponent
{
    public OpenFlowTemplateComponent()
        : base(
            "Open Flow Template",
            "Template",
            "Opens the Crowd Flow starter template. Click the button to open.",
            "Crowd Flow",
            "Crowd")
    {
    }

    public override Guid ComponentGuid => new("f7921ce7-0365-415d-a019-7be4a05b0bb2");

    protected override bool IsDeveloperOnly => false;

    protected override Bitmap? Icon => Properties.Resources.CrowdTemplate;

    protected override GH_Exposure DefaultExposure => GH_Exposure.primary;

    public override void CreateAttributes() => m_attributes = new ButtonAttributes(this);

    protected override void RegisterInputParams(GH_InputParamManager pManager) { }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }

    protected override void SolveInstance(IGH_DataAccess DA) { }

    internal void OpenTemplate()
    {
        string? path = FindTemplatePath();
        if (path == null)
        {
            MessageBox.Show(
                "Flow template not found.\nRun the deploy or install the plugin first.",
                "Crowd Flow",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var io = new GH_DocumentIO();
            if (io.Open(path))
            {
                var doc = io.Document;
                if (doc != null)
                {
                    Instances.DocumentServer?.AddDocument(doc);
                    if (Instances.ActiveCanvas != null)
                    {
                        Instances.ActiveCanvas.Document = doc;
                        Instances.ActiveCanvas.Refresh();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Crowd Flow", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string? FindTemplatePath()
    {
        // 1. Relative to plugin DLL — works for both local deploy and Yak versioned installs.
        // Yak puts DLL in <version>\net8.0\, templates land in <version>\templates\.
        string? pluginDir = Path.GetDirectoryName(typeof(OpenFlowTemplateComponent).Assembly.Location);
        if (!string.IsNullOrEmpty(pluginDir))
        {
            string candidate = Path.GetFullPath(Path.Combine(pluginDir, "..", "templates", "Flow.gh"));
            if (File.Exists(candidate)) return candidate;
        }

        // 2. AppData CrowdFlow root (local deploy target).
        string appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Grasshopper", "Libraries", "CrowdFlow", "templates", "Flow.gh");
        return File.Exists(appData) ? appData : null;
    }

    private sealed class ButtonAttributes : GH_ComponentAttributes
    {
        private RectangleF _buttonRect;

        public ButtonAttributes(OpenFlowTemplateComponent owner) : base(owner) { }

        protected override void Layout()
        {
            base.Layout();
            var b = Bounds;
            if (b.Width < 160f)
            {
                float dx = 160f - b.Width;
                b.X -= dx / 2f;
                b.Width = 160f;
            }
            b.Height += 24;
            Bounds = b;
            _buttonRect = new RectangleF(b.X + 3, b.Bottom - 26, b.Width - 6, 22);
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            if (channel != GH_CanvasChannel.Objects) return;

            var button = GH_Capsule.CreateTextCapsule(
                _buttonRect, _buttonRect,
                GH_Palette.Black, "Open Template", 2, 0);
            button.Render(graphics, Selected, Owner.Locked, false);
            button.Dispose();
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && _buttonRect.Contains(e.CanvasLocation))
            {
                ((OpenFlowTemplateComponent)Owner).OpenTemplate();
                return GH_ObjectResponse.Handled;
            }
            return base.RespondToMouseDown(sender, e);
        }
    }
}
