using System;
using System.Collections.Generic;
using HollowCreek.Core;
using HollowCreek.Core.Puzzles;
using HollowCreek.Core.State;
using HollowCreek.Gameplay.Dialogue;
using HollowCreek.Gameplay.Puzzles;
using HollowCreek.UI.Puzzles;
using HollowCreek.Gameplay.Input;
using HollowCreek.Gameplay.Inspection;
using HollowCreek.Gameplay.Interaction;
using HollowCreek.Gameplay.Locations;
using HollowCreek.Gameplay.Messages;
using HollowCreek.Gameplay.Modals;
using HollowCreek.UI.Common;
using HollowCreek.UI.Hud;
using HollowCreek.UI.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace HollowCreek.UI
{
    /// <summary>
    /// Собирает интерфейс игры: находит в разметке нужные части и создаёт для них контроллеры.
    /// Живёт в сцене Bootstrap вместе с UIDocument.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(-800)]
    public sealed class UIRoot : MonoBehaviour
    {
        readonly List<IDisposable> views = new();
        MessageScreen messages;

        void Start()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            // Контейнеры шаблонов не должны перехватывать мышь — только сами панели.
            root.Query<TemplateContainer>().ForEach(t => t.pickingMode = PickingMode.Ignore);

            var modals = Services.Get<ModalStack>();
            var input = Services.Get<GameInput>();
            var state = Services.Get<GameState>();

            views.Add(new StaticTextLocalizer(root));
            views.Add(new LocationTransitionView(root.Q("fader"), root.Q<Label>("location-title"), Services.Get<LocationLoader>()));
            views.Add(new HudView(root.Q("hud"), Services.Get<Interactor>(), modals, input, state));
            views.Add(new InspectionOverlay(root.Q("inspection"), Services.Get<InspectionController>(), input));
            views.Add(new NotebookScreen(root.Q("notebook"), modals, input, state));
            var picker = new EvidencePickerScreen(root.Q("evidence-picker"), modals, state);
            views.Add(picker);
            views.Add(new DialogueScreen(root.Q("dialogue"), modals, Services.Get<DialogueService>(), picker));
            views.Add(new PuzzleScreen(root.Q("puzzle"), modals, Services.Get<PuzzleService>(),
                new Dictionary<Type, Func<IPuzzleView>>
                {
                    [typeof(CodeLockPuzzle)] = () => new CodeLockView(),
                    [typeof(SequencePuzzle)] = () => new SequenceView(),
                    [typeof(RubbingPuzzle)] = () => new RubbingView(input),
                }));
            messages = new MessageScreen(root.Q("message"), modals);
            views.Add(messages);
            Services.Register<IMessagePresenter>(messages);
        }

        void OnDestroy()
        {
            if (messages != null) Services.Unregister<IMessagePresenter>(messages);
            foreach (var view in views) view.Dispose();
            views.Clear();
        }
    }
}
