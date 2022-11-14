using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using HarmonyLib;
using NeosModLoader;
using System;

namespace ComponentHunter
{
    public class ComponentHunter : NeosMod
    {
        public static ModConfiguration config;
        public override string Name => "ComponentHunter";
        public override string Author => "dfgHiatus";
        public override string Version => "1.0.0";
        public override string Link => "https://github.com/dfgHiatus/ComponentHunter/";

        public override void OnEngineInit()
        {
            new Harmony("net.dfgHiatus.ComponentHunter").PatchAll();
            config = GetConfiguration();
            Engine.Current.RunPostInit(() =>
            {
                DevCreateNewForm.AddAction("Editor", "Component Hunter", GenerateComponentHunterUI);
            });
        }

        public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
        {
            builder
                .Version(new Version(1, 0, 0))
                .AutoSave(true);
        }

        internal class wizInstance
        {
            internal ReferenceField<Slot> ProcessRoot;
            internal ReferenceField<Component> FoundComponent;
            internal TypeField ComponentType;
            internal ValueField<bool> SearchParents;
            internal ValueField<bool> Run;

            internal void Search(SyncField<bool> state)
            {
                if (!state.Value || state.World.GetUserByAllocationID(state.ReferenceID.User) != state.World.LocalUser) return; // Only run on enable

                if (SearchParents.Value)
                    FoundComponent.Reference.Target = ProcessRoot.Reference.Target.GetComponentInParents(ComponentType.Type.Value);
                else
                    FoundComponent.Reference.Target = ProcessRoot.Reference.Target.GetComponentInChildren(ComponentType.Type.Value);

                state.World.RunInUpdates(0, () => state.Value = false); // Reset
            }
        }

        private static void GenerateComponentHunterUI(Slot s)
        {
            var wizard = new wizInstance();

            s.PersistentSelf = false;
            s.GetComponentInChildrenOrParents<Canvas>()?.MarkDeveloper();
            s.AttachComponent<DestroyOnUserLeave>().TargetUser.Target = Engine.Current.WorldManager.FocusedWorld.LocalUser;

            Slot Data = s.AddSlot("Data");
            wizard.ProcessRoot = Data.AddSlot("ProcessRoot").AttachComponent<ReferenceField<Slot>>();
            wizard.ProcessRoot.Reference.Target = null;
            wizard.ComponentType = Data.AddSlot("ComponentType").AttachComponent<TypeField>();
            wizard.ComponentType.Type.Value = null;
            wizard.SearchParents = Data.AddSlot("SearchParents").AttachComponent<ValueField<bool>>();
            wizard.SearchParents.Value.Value = false;

            var neosCanvasPanel = s.AttachComponent<NeosCanvasPanel>();
            neosCanvasPanel.Panel.AddCloseButton();
            neosCanvasPanel.Panel.AddParentButton();
            neosCanvasPanel.Panel.TitleField.Value = "Component Hunter";
            neosCanvasPanel.CanvasSize = new float2(525f, 525f);
            neosCanvasPanel.PhysicalHeight = 0.5f;
            var ui = new UIBuilder(neosCanvasPanel.Canvas);

            ui.ScrollArea();
            ui.VerticalLayout(4f, 8f, childAlignment: Alignment.TopLeft);
            ui.FitContent(SizeFit.Disabled, SizeFit.MinSize);
            ui.Style.MinHeight = 32f;
            ui.Text("Component Hunter");
            ui.Spacer(24f);

            ui.Text("Process root: ");
            ui.Next("Process root: ");
            ui.Current.AttachComponent<RefEditor>().Setup(wizard.ProcessRoot.Reference);
            ui.Spacer(24f);

            ui.Text("Component to search for: ");
            ui.Next("Component to search for: ");
            SyncMemberEditorBuilder.BuildField(wizard.ComponentType.Type, null, ui.Current);
            ui.Spacer(24f);

            ui.HorizontalElementWithLabel("Search in parents (instead of children)", 0.9f, () => ui.BooleanMemberEditor(wizard.SearchParents.Value));

            ui.Spacer(24f);

            wizard.Run = Data.AddSlot("Run").AttachComponent<ValueField<bool>>();
            wizard.Run.Value.OnValueChange += wizard.Search;
            ui.Button("Show this component in this hierarchy").Slot.AttachComponent<ButtonToggle>().TargetValue.Target = wizard.Run.Value;
            ui.Spacer(24f);

            ui.Text("Found Component: ");
            ui.Next("Found Component: ");
            wizard.FoundComponent = Data.AddSlot("FoundComponent").AttachComponent<ReferenceField<Component>>();
            ui.Current.AttachComponent<RefEditor>().Setup(wizard.FoundComponent.Reference);
            ui.Spacer(24f);
        }
    }
}