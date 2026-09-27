using UnityEngine.UIElements;

namespace HollowCreek.UI.Common
{
    /// <summary>
    /// Печатает текст в надписи постепенно, как будто персонаж говорит. Щелчок по надписи
    /// (или вызов <see cref="Complete"/>) показывает текст сразу целиком.
    /// </summary>
    public sealed class Typewriter
    {
        const float CharactersPerSecond = 90f;

        readonly Label label;
        string fullText = string.Empty;
        float shown;
        readonly IVisualElementScheduledItem ticker;

        public Typewriter(Label label)
        {
            this.label = label;
            ticker = label.schedule.Execute(Tick).Every(16);
            ticker.Pause();
            label.RegisterCallback<PointerDownEvent>(_ => Complete());
        }

        public bool IsTyping => shown < fullText.Length;

        public void Show(string text)
        {
            fullText = text ?? string.Empty;
            shown = 0f;
            label.text = string.Empty;
            ticker.Resume();
        }

        public void Complete()
        {
            shown = fullText.Length;
            label.text = fullText;
            ticker.Pause();
        }

        void Tick(TimerState timer)
        {
            shown += timer.deltaTime / 1000f * CharactersPerSecond;
            if (shown >= fullText.Length)
            {
                Complete();
                return;
            }
            label.text = fullText.Substring(0, (int)shown);
        }
    }
}
