using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class SimpleGraphicTween : MonoBehaviour
{
    [SerializeField] private Graphic _graphic;

    [SerializeField] private bool _playOnAwake;

    [SerializeField, Min(0f)] private float _duraiton = 1f;
    [SerializeField] private Ease _ease = Ease.Unset;
    [SerializeField] private int _loopCount = -1;
    [SerializeField] private LoopType _loopType = LoopType.Incremental;

    [Header("Do not tween if equal")]
    [SerializeField] private Vector3 _startPos = Vector3.zero;
    [SerializeField] private Vector3 _endPos = Vector3.zero;
    [Space(5f)]
    [SerializeField] private Vector3 _startRot = Vector3.zero;
    [SerializeField] private Vector3 _endRot = Vector3.zero;
    [Space(5f)]
    [SerializeField] private Vector3 _startScale = Vector3.one;
    [SerializeField] private Vector3 _endScale = Vector3.one;
    [Space(5f)]
    [SerializeField, Range(0f, 1f)] private float _startAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private  float _endAlpha = 1f;
    [Space(5f)]
    [SerializeField] private Color _startColor = Color.white;
    [SerializeField] private Color _endColor = Color.white;

    [Header("Image only available")]
    [SerializeField] private Gradient _gradient;

    private Sequence _sequence;
    private bool _isPlayed;
    public Action OnCompleted;

    protected void Start()
    {
        if (_playOnAwake) GetSequence().Play();
    }

    private void OnEnable()
    {
        if (_isPlayed) GetSequence().Play();
    }

    private void OnDisable()
    {
        _isPlayed = _sequence != null && _sequence.IsPlaying();
        _sequence?.Pause();
    }

    public void Play() => GetSequence().Play();
    public void Restart() => GetSequence().Restart();
    public void Puase() => _sequence?.Pause();
    public void Kill() => _sequence?.Kill();

    private Sequence GetSequence()
    {
        if (_sequence != null) return _sequence;

        _sequence = DOTween.Sequence()
            .SetEase(Ease.Linear)
            .SetLoops(_loopCount, _loopType)
            .OnComplete(() => OnCompleted?.Invoke())
            .SetAutoKill(false)
            .SetLink(gameObject);

        if (!_startPos.Equals(_endPos))
        {
            _graphic.rectTransform.DOAnchorPos(_startPos, 0f);
            _sequence.Join(_graphic.rectTransform.DOAnchorPos(_endPos, _duraiton).SetEase(_ease));
        }

        if (!_startRot.Equals(_endRot))
        {
            _graphic.rectTransform.DOLocalRotate(_startRot, 0f);
            _sequence.Join(_graphic.rectTransform.DOLocalRotate(_endRot, _duraiton, RotateMode.FastBeyond360).SetEase(_ease));
        }

        if (!_startScale.Equals(_endScale))
        {
            _graphic.rectTransform.DOScale(_startScale, 0f);
            _sequence.Join(_graphic.rectTransform.DOScale(_endScale, _duraiton).SetEase(_ease));
        }

        if (!_startAlpha.Equals(_endAlpha))
        {
            _graphic.DOFade(_startAlpha, 0f);
            _sequence.Join(_graphic.DOFade(_endAlpha, _duraiton).SetEase(_ease));
        }

        if (!_startColor.Equals(_endColor))
        {
            _graphic.DOColor(_startColor, 0f);
            _sequence.Join(_graphic.DOColor(_endColor, _duraiton).SetEase(_ease));
        }

        if (!_gradient.Equals(new Gradient()) && _gradient.GetType() == typeof(Image))
        {
            _sequence.Join((_graphic as Image).DOGradientColor(_gradient, _duraiton).SetEase(_ease));
        }

        return _sequence;
    }

#if UNITY_EDITOR

    private void OnValidate()
    {
        _graphic ??= GetComponent<Graphic>();
    }

#endif
}
