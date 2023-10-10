using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using static FrooxEngine.ObjectGridAligner;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Linq;

namespace PhotoManager;

public class PhotoManager : ResoniteMod
{
    public override string Name => "PhotoManager";
    public override string Author => "dfgHiatus";
    public override string Version => "2.0.0";
    public override string Link => "https://github.com/dfgHiatus/PhotoManager/";
    public override void OnEngineInit()
    {
        new Harmony("net.dfgHiatus.PhotoManager").PatchAll();
        config = GetConfiguration();
        Engine.Current.RunPostInit(() => {
            DevCreateNewForm.AddAction("Editor", "Photo Manager", GeneratePhotoManagerUI);
        });
    }

    private static ReferenceField<Slot> ProcessRoot;
    private static ReferenceField<Slot> NewParent;
    private static Text ResultsText;
    public static ModConfiguration config;

    // Default ObjectGridAligner values
    [AutoRegisterConfigKey]
    public readonly static ModConfigurationKey<bool> autoAddChildren
        = new("autoAddChildren", "Auto Add Children", () => true);
    [AutoRegisterConfigKey]
    public readonly static ModConfigurationKey<Align> horizontalAlignment
        = new("horizontalAlignment", "Horizontal Alignment", () => Align.Pos);
    [AutoRegisterConfigKey]
    public readonly static ModConfigurationKey<Align> verticalAlignment
        = new("verticalAlignment", "Vertical Alignment", () => Align.Mid);
    [AutoRegisterConfigKey]
    public readonly static ModConfigurationKey<float2> cellSize
        = new("cellSize", "Cell Size", () => new float2(0.25f, 0.25f));
    [AutoRegisterConfigKey]
    public readonly static ModConfigurationKey<AxisDir> rowAxis
        = new("rowAxis", "Row Axis", () => AxisDir.Ypos);
    [AutoRegisterConfigKey]
    public readonly static ModConfigurationKey<AxisDir> columnAxis
        = new("columnAxis", "Column Axis", () => AxisDir.Xpos);
    [AutoRegisterConfigKey]
    public readonly static ModConfigurationKey<int> defaultitemsPerRow
        = new("defaultitemsPerRow", "Default Items Per Row", () => 4);
    [AutoRegisterConfigKey]
    public readonly static ModConfigurationKey<float> lerpSpeed 
        = new("lerpSpeed", "Leap Speed", () => 1.5f);

    private static void GeneratePhotoManagerUI (Slot s)
    {
        s.PersistentSelf = false;
        s.GetComponentInChildrenOrParents<Canvas>()?.MarkDeveloper();
        s.AttachComponent<DestroyOnUserLeave>().TargetUser.Target = Engine.Current.WorldManager.FocusedWorld.LocalUser;

        Slot Data = s.AddSlot("Data");
        ProcessRoot = Data.AddSlot("ProcessRoot").AttachComponent<ReferenceField<Slot>>();
        ProcessRoot.Reference.Target = null;
        NewParent = Data.AddSlot("NewParent").AttachComponent<ReferenceField<Slot>>();
        NewParent.Reference.Target = null;

        var resoniteCanvasPanel = s.AttachComponent<LegacyCanvasPanel>();
        resoniteCanvasPanel.Panel.AddCloseButton();
        resoniteCanvasPanel.Panel.TitleField.Value = "Photo Manager";
        resoniteCanvasPanel.CanvasSize = new float2(600f, 400f);
        resoniteCanvasPanel.PhysicalHeight = 0.5f;
        var ui = new UIBuilder(resoniteCanvasPanel.Canvas);

        ui.ScrollArea();
        ui.VerticalLayout(4f, 8f, childAlignment: Alignment.TopLeft);
        ui.FitContent(SizeFit.Disabled, SizeFit.MinSize);
        ui.Style.MinHeight = 32f;
        ui.Text("Photo Manager");
        ui.Spacer(24f);

        ui.Text("Process root: ");
        ui.Next("Process root: ");
        ui.Current.AttachComponent<RefEditor>().Setup(ProcessRoot.Reference);
        ui.Spacer(24f);

        ui.Text("New parent: ");
        ui.Next("New parent: ");
        ui.Current.AttachComponent<RefEditor>().Setup(NewParent.Reference);
        ui.Spacer(24f);

        ui.Button("Move all screenshots under \"Process Root\" to \"New Parent\"").LocalPressed += CollectPhotos;

        ui.Spacer(24f);
        ResultsText = ui.Text("Photos found: ---");
        ui.Spacer(24f);
    }

    private static void CollectPhotos (IButton button, ButtonEventData eventData)
    {
        if (ProcessRoot.Reference.Target is null)
        {
            ResultsText.Content.Value = "Please specify a process root";
            return;
        }
        if (NewParent.Reference.Target is null)
        {
            ResultsText.Content.Value = "Please specify a new parent";
            return;
        }

        ObjectGridAligner aligner;
        var possibleAligner = NewParent.Reference.Target.GetComponent<ObjectGridAligner>();
        if (possibleAligner is null)
        {
            aligner = NewParent.Reference.Target.AttachComponent<ObjectGridAligner>();
        }
        else
        {
            aligner = possibleAligner;
        }

        PreparePhotoAligner(aligner);
        possibleAligner.Slot.LocalScale = float3.One;
        var photos = ProcessRoot.Reference.Target.GetComponentsInChildren<PhotoMetadata>().Select(x => x.Slot);
        var photosCount = photos.Count();

        foreach (var photo in photos)
        {
            photo.SetParent(NewParent.Reference.Target, false);
            photo.LocalRotation = floatQ.Identity;
            photo.LocalScale = float3.One;
        }

        ResultsText.Content.Value = $"Found {photosCount} photos to reparent";
    }

    private static void PreparePhotoAligner(ObjectGridAligner aligner)
    {
        aligner.AutoAddChildren.Value = config.GetValue(autoAddChildren);
        aligner.HorizontalAlignment.Value = config.GetValue(horizontalAlignment);
        aligner.VerticalAlignment.Value = config.GetValue(verticalAlignment);
        aligner.CellSize.Value = config.GetValue(cellSize);
        aligner.RowAxis.Value = config.GetValue(rowAxis);
        aligner.ColumnAxis.Value = config.GetValue(columnAxis);
        aligner.ItemsPerRow.Value = config.GetValue(defaultitemsPerRow);
        aligner.LerpSpeed.Value = config.GetValue(lerpSpeed);
    }
}