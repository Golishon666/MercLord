using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MercLord.Game.UI
{
    public sealed class MainMenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private MainMenuSceneRoot sceneRoot;

        public MainMenuSceneRoot SceneRoot => sceneRoot;

        protected override void Configure(IContainerBuilder builder)
        {
            if (sceneRoot == null)
            {
                throw new InvalidOperationException("MainMenuLifetimeScope requires a MainMenuSceneRoot reference.");
            }

            builder.RegisterComponent(sceneRoot);
        }
    }
}
