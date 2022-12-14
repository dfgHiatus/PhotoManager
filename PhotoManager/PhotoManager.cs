using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using static FrooxEngine.ObjectGridAligner;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Linq;

namespace PhotoManager
{
    public class PhotoManager : NeosMod
    {
        public override string Name => "PhotoManager";
        public override string Author => "dfgHiatus";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/dfgHiatus/PhotoManager/";
        public override void OnEngineInit()
        {
            new Harmony("net.dfgHiatus.PhotoManager").PatchAll();
            config = GetConfiguration();
            Engine.Current.RunPostInit(() => {
                DevCreateNewForm.AddAction("Editor", "Photo Manager", GeneratePhotoManagerUI);
            });
        }

        public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
        {
            builder
                .Version(new Version(1, 0, 0))
                .AutoSave(true);
        }

        private static ReferenceField<Slot> ProcessRoot;
        private static ReferenceField<Slot> NewParent;
        private static Text ResultsText;
        public static ModConfiguration config;

        // Default ObjectGridAligner values
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<bool> autoAddChildren
            = new ModConfigurationKey<bool>("autoAddChildren", "Auto Add Children", () => true);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<Align> horizontalAlignment
            = new ModConfigurationKey<Align>("horizontalAlignment", "Horizontal Alignment", () => Align.Pos);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<Align> verticalAlignment
            = new ModConfigurationKey<Align>("verticalAlignment", "Vertical Alignment", () => Align.Mid);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<float2> cellSize
            = new ModConfigurationKey<float2>("cellSize", "Cell Size", () => new float2(0.25f, 0.25f));
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<AxisDir> rowAxis
            = new ModConfigurationKey<AxisDir>("rowAxis", "Row Axis", () => AxisDir.Ypos);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<AxisDir> columnAxis
            = new ModConfigurationKey<AxisDir>("columnAxis", "Column Axis", () => AxisDir.Xpos);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<int> defaultitemsPerRow
            = new ModConfigurationKey<int>("defaultitemsPerRow", "Default Items Per Row", () => 4);
        [AutoRegisterConfigKey]
        public static ModConfigurationKey<float> lerpSpeed 
            = new ModConfigurationKey<float>("lerpSpeed", "Lerp Speed", () => 1.5f);

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

            var neosCanvasPanel = s.AttachComponent<NeosCanvasPanel>();
            neosCanvasPanel.Panel.AddCloseButton();
            neosCanvasPanel.Panel.TitleField.Value = "Photo Manager";
            neosCanvasPanel.CanvasSize = new float2(600f, 400f);
            neosCanvasPanel.PhysicalHeight = 0.5f;
            var ui = new UIBuilder(neosCanvasPanel.Canvas);

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
}