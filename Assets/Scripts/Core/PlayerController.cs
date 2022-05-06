using Core.Interfaces;
using Zenject;

namespace Core
{
    public class PlayerController : IPlayerController
    {
        private readonly Player.Factory _mainCharacterFactory;

        private Player _player;

        [Inject]
        private PlayerController(Player.Factory mainCharacterFactory)
        {
            _mainCharacterFactory = mainCharacterFactory;
            CreateMainCharacter();
        }

        public void CreateMainCharacter()
        {
            _player = _mainCharacterFactory.Create();
        }
    }
}