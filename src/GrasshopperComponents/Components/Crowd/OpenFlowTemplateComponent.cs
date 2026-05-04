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

/// <summary>
/// Button component that opens the bundled Flow.gh crowd simulation starter template.
/// Templates are resolved relative to the plugin DLL so the path works for both
/// local deploy and Yak versioned installs without hard-coded paths.
/// </summary>
public sealed class OpenFlowTemplateComponent : IndGhComponent
{
    private const string TemplateFileName = "Flow.gh";

    public OpenFlowTemplateComponent()
        : base(
            "Open Flow Template",
            "Template",
            "Opens the Crowd Flow starter template (Flow.gh).\nClick the button on the component face.",
            "Crowd Flow",
            "Crowd")
    {
    }

    public override Guid ComponentGuid => new("f7921ce7-0365-415d-a019-7be4a05b0bb2");

    protected override bool IsDeveloperOnly => false;

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.CrowdTemplate;

    protected override GH_Exposure DefaultExposure => GH_Exposure.secondary;

    public override void CreateAttributes()
    {
        m_attributes = new OpenFlowTemplateAttributes(this);
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        // No inputs — launcher component
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Template Path", "P", "Path to the opened template file.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var path = FindTemplatePath();
        if (path == null)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                $"{TemplateFileName} not found. Expected at: {GetTemplatesDirectory()}");
            return;
        }
        DA.SetData(0, path);
    }

    internal void OpenTemplate()
    {
        var path = FindTemplatePath();
        if (path == null)
        {
            MessageBox.Show(
                $"{TemplateFileName} not found.\nExpected directory: {GetTemplatesDirectory()}\n\n" +
                "Run the Crowd Flow installer to restore template files.",
                "Crowd Flow",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var io = new GH_DocumentIO();
            if (!io.Open(path))
            {
                MessageBox.Show(
                    $"Failed to open template:\n{path}",
                    "Crowd Flow",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var doc = io.Document;
            if (doc == null)
                return;

            var server = Instances.DocumentServer;
            if (server == null)
                return;

            server.AddDocument(doc);

            if (Instances.ActiveCanvas != null)
            {
                Instances.ActiveCanvas.Document = doc;
                Instances.ActiveCanvas.Refresh();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Error opening template:\n{ex.Message}",
                "Crowd Flow",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Returns the templates directory. Checks relative to the plugin DLL first
    /// (works for both local deploy and Yak versioned installs), then falls back
    /// to the AppData CrowdFlow root.
    /// </summary>
    private static string GetTemplatesDirectory()
    {
        // Local deploy:  %APPDATA%\Grasshopper\Libraries\CrowdFlow\net8.0\  -> ../templates
        // Yak install:   %APPDATA%\Grasshopper\Libraries\CrowdFlow\0.1.3.0\net8.0\ -> ../templates
        var assemblyDir = Path.GetDirectoryName(typeof(OpenFlowTemplateComponent).Assembly.Location);
        if (assemblyDir != null)
        {
            var candidate = Path.GetFullPath(Path.Combine(assemblyDir, "..", "templates"));
            if (Directory.Exists(candidate))
                return candidate;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Grasshopper", "Libraries", "CrowdFlow", "templates");
    }

    private static string? FindTemplatePath()
    {
        var path = Path.Combine(GetTemplatesDirectory(), TemplateFileName);
        return File.Exists(path) ? path : null;
    }

    private sealed class OpenFlowTemplateAttributes : GH_ComponentAttributes
    {
        private const int ButtonHeight = 22;
        private const string ButtonText = "Open Template";
        private RectangleF _buttonBounds;

        public OpenFlowTemplateAttributes(OpenFlowTemplateComponent owner)
            : base(owner)
        {
        }

        private OpenFlowTemplateComponent TemplateOwner => (OpenFlowTemplateComponent)Owner;

        protected override void Layout()
        {
            base.Layout();
            var bounds = Bounds;
            bounds.Height += ButtonHeight;
            Bounds = bounds;
            _buttonBounds = new RectangleF(
                Bounds.Left,
                Bounds.Bottom - ButtonHeight,
                Bounds.Width,
                ButtonHeight);
        }

        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            base.Render(canvas, graphics, channel);
            if (channel != GH_CanvasChannel.Objects)
                return;

            var rect = GH_Convert.ToRectangle(_buttonBounds);
            var capsule = GH_Capsule.CreateTextCapsule(
                rect, rect, GH_Palette.Black, ButtonText, 2, 0);
            capsule.Render(graphics, Selected, Owner.Locked, false);
            capsule.Dispose();
        }

        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == MouseButtons.Left && _buttonBounds.Contains(e.CanvasLocation))
            {
                TemplateOwner.OpenTemplate();
                return GH_ObjectResponse.Handled;
            }
            return base.RespondToMouseDown(sender, e);
        }
    }
}
