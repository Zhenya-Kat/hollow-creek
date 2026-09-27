using System.Threading.Tasks;

namespace HollowCreek.Gameplay.Menus
{
    public enum MainMenuChoice
    {
        Continue,
        NewGame,
    }

    /// <summary>
    /// Главное меню. Показывает его слой интерфейса; игровой код только ждёт выбора игрока.
    /// </summary>
    public interface IMainMenu
    {
        Task<MainMenuChoice> ShowAsync(bool canContinue);
    }
}
