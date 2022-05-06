using Core;
using UnityEngine;
using Zenject;

namespace Infrastructure
{
    public class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private Player _mainCharacter;
        
        public override void InstallBindings()
        {
            BindPlayerFactory();
            BindPlayerController();
        }

        private void BindPlayerFactory()
        {
            Container.BindFactory<Player, Player.Factory>()
                .FromComponentInNewPrefab(_mainCharacter)
                .NonLazy();
        }

        private void BindPlayerController()
        {
            Container.Bind<PlayerController>()
                .AsSingle()
                .NonLazy();
        }
    }
}