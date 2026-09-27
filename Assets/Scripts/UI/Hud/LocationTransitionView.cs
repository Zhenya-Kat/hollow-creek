using System;
using HollowCreek.Gameplay.Locations;
using HollowCreek.UI.Common;
using UnityEngine.UIElements;

namespace HollowCreek.UI.Hud
{
    /// <summary>
    /// Затемнение экрана при смене локации и название локации при входе («Дом Элис»).
    /// </summary>
    public sealed class LocationTransitionView : IDisposable
    {
        const string FaderClearClass = "fader--clear";
        const string TitleVisibleClass = "location-title--visible";
        const long TitleShowMs = 2600;

        readonly VisualElement fader;
        readonly Label title;
        readonly LocationLoader locations;
        IVisualElementScheduledItem hideTitle;

        public LocationTransitionView(VisualElement fader, Label title, LocationLoader locations)
        {
            this.fader = fader;
            this.title = title;
            this.locations = locations;
            locations.LoadingStarted += OnLoadingStarted;
            locations.LocationLoaded += OnLocationLoaded;
            // Если локация уже загружена к моменту создания интерфейса — сразу показываем картинку.
            if (locations.Current != null) fader.AddToClassList(FaderClearClass);
        }

        public void Dispose()
        {
            locations.LoadingStarted -= OnLoadingStarted;
            locations.LocationLoaded -= OnLocationLoaded;
        }

        void OnLoadingStarted(Core.Data.LocationDefinition _)
        {
            fader.RemoveFromClassList(FaderClearClass);
            title.RemoveFromClassList(TitleVisibleClass);
        }

        void OnLocationLoaded(LocationRoot root, SpawnPoint _)
        {
            // Проясняем в следующем кадре, чтобы игрок уже стоял на месте.
            fader.schedule.Execute(() => fader.AddToClassList(FaderClearClass));

            title.text = UIText.Get(root.Location.DisplayName);
            if (string.IsNullOrEmpty(title.text)) return;
            title.AddToClassList(TitleVisibleClass);
            hideTitle?.Pause();
            hideTitle = title.schedule.Execute(() => title.RemoveFromClassList(TitleVisibleClass)).StartingIn(TitleShowMs);
        }
    }
}
