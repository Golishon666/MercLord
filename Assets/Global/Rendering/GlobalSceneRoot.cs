using System;
using MercLord.Game.Save;
using UnityEngine;
using VContainer;

namespace MercLord.Global.Rendering
{
    public sealed class GlobalSceneRoot : MonoBehaviour
    {
        [SerializeField] private PlanetRenderer planetRenderer;
        private ISaveService saveService;
        private IGlobalMapPresenter globalMapPresenter;

        public PlanetRenderer PlanetRenderer => planetRenderer;

        [Inject]
        public void Construct(ISaveService saveService, IGlobalMapPresenter globalMapPresenter)
        {
            this.saveService = saveService ?? throw new ArgumentNullException(nameof(saveService));
            this.globalMapPresenter = globalMapPresenter ?? throw new ArgumentNullException(nameof(globalMapPresenter));
        }

        private void Start()
        {
            if (planetRenderer == null)
            {
                throw new InvalidOperationException("GlobalSceneRoot requires a PlanetRenderer reference.");
            }

            if (saveService?.Current?.World == null)
            {
                throw new InvalidOperationException("GlobalSceneRoot cannot render without an active world save.");
            }

            if (globalMapPresenter == null)
            {
                throw new InvalidOperationException("GlobalSceneRoot requires IGlobalMapPresenter injection.");
            }

            globalMapPresenter.Render(planetRenderer, saveService.Current.World);
        }
    }
}
