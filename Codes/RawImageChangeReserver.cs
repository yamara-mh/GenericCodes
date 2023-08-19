using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Yamara
{
    [RequireComponent(typeof(RawImage))]
    public class RawImageChangeReserver : MonoBehaviour
    {
        [SerializeField] private RawImage RawImage;
        [SerializeField] private Texture _loadingTexture;
        [SerializeField] private Texture _errorTexture;
        [SerializeField] private bool _cancelOnDisable;
        
        private IDisposable _reserveDisposable;

        private void OnDisable()
        {
            if (_cancelOnDisable) CancelReservation();
        }

        public void SetTexture(Texture texture)
        {
            CancelReservation();
            if (texture != null) RawImage.texture = texture;
            else if (_errorTexture != null) RawImage.texture = _errorTexture;
        }
        public void SetLoadingTexture()
        {
            CancelReservation();
            RawImage.texture = _loadingTexture;
        }
        public void SetErrorTexture()
        {
            CancelReservation();
            RawImage.texture = _errorTexture;
        }
        
        public void Reserve(IObservable<Texture> observable)
        {
            CancelReservation();
            if (_loadingTexture != null) RawImage.texture = _loadingTexture;
            _reserveDisposable = observable.Subscribe(SetTexture);
        }

        public void CancelReservation() => _reserveDisposable?.Dispose();
    }
}
