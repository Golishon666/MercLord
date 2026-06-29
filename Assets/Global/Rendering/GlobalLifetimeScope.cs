using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalLifetimeScope : LifetimeScope
    {
        [SerializeField] private GlobalSceneRoot sceneRoot;

        protected override void Configure(IContainerBuilder builder)
        {
            if (sceneRoot == null)
            {
                throw new InvalidOperationException("GlobalLifetimeScope requires a GlobalSceneRoot reference.");
            }

            builder.RegisterComponent(sceneRoot);
        }
    }
}
