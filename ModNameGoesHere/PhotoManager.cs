using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using NeosModLoader;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ModNameGoesHere
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
            Engine.Current.RunPostInit(() => {
                DevCreateNewForm.AddAction("Editor", "Photo Manager", PhotoOrganizer);
            });
        }

        private static ReferenceField<Slot> ProcessRoot;
        private static ReferenceField<Slot> NewParent;
        private static Text ResultsText;

        private static void PhotoOrganizer (Slot s)
        {
            s.GetComponentInChildrenOrParents<Canvas>()?.MarkDeveloper();
            s.AttachComponent<DestroyOnUserLeave>().TargetUser.Target = Engine.Current.WorldManager.FocusedWorld.LocalUser;

            Slot Data = s.AddSlot("Data");
            ProcessRoot = Data.AddSlot("ProcessRoot").AttachComponent<ReferenceField<Slot>>();
            ProcessRoot.Reference.Target = null;
            NewParent = Data.AddSlot("NewParent").AttachComponent<ReferenceField<Slot>>();
            NewParent.Reference.Target = null;
            s.PersistentSelf = false;

            var neosCanvasPanel = s.AttachComponent<NeosCanvasPanel>();
            neosCanvasPanel.Panel.AddCloseButton();
            neosCanvasPanel.Panel.TitleField.Value = "Photo Manager";
            neosCanvasPanel.CanvasSize = new float2(800f, 1024f);
            neosCanvasPanel.PhysicalHeight = 0.5f;
            var ui = new UIBuilder(neosCanvasPanel.Canvas);

            Msg("ui");
            ui.ScrollArea();
            ui.VerticalLayout(4f, 8f, childAlignment: Alignment.TopLeft);
            ui.FitContent(SizeFit.Disabled, SizeFit.MinSize);
            ui.Style.MinHeight = 32f;
            ui.Text("Photo Manager");
            ui.Spacer(24f);

            ui.Text("Process Root: ");
            ui.Next("Process Root: ");
            ui.Current.AttachComponent<RefEditor>().Setup(ProcessRoot.Reference);
            ui.Spacer(24f);

            ui.Text("New Parent: ");
            ui.Next("New Parent: ");
            ui.Current.AttachComponent<RefEditor>().Setup(NewParent.Reference);
            ui.Spacer(24f);

            ui.Button("Move all screenshots under \"Process Root\" to \"New Parent\"").LocalPressed += (IButton button, ButtonEventData eventData) => 
            {
                if (ProcessRoot.Reference.Slot is null)
                    throw new ArgumentException("ProcessRoot.Reference.Slot was null");
                if (NewParent.Reference.Slot is null)
                    throw new ArgumentException("NewParent.Reference.Slot was null");

                // var photos = ProcessRoot.Reference.Slot.GetAllChildren().Where(x => x.GetComponent<PhotoMetadata> != null).ToList();
                var photos = ProcessRoot.Reference.Slot.GetComponentsInChildren<PhotoMetadata>().Select(x => x.Slot).ToList();
                var photosCount = photos.Count;

                foreach (var photo in photos)
                {
                    photo.SetParent(NewParent.Reference.Slot, false);
                }

                ResultsText.Content.Value = $"Found {photos.Count} photos to reparent";
            };

            ui.Spacer(24f);
            ResultsText = ui.Text("Photos found: ---");
            ui.Spacer(24f);
        }
    }
}