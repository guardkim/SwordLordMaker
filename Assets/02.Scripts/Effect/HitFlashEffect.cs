using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HitFlashEffect : MonoBehaviour
{
    [Header("색상 설정")]
    [SerializeField] private Color _hitColor = Color.red;
    [SerializeField] private Color _flashColor = Color.white;

    [Header("타이밍 설정")]
    [SerializeField] private float _hitDuration = 0.1f;
    [SerializeField] private float _flashDuration = 0.1f;
    [SerializeField] private float _recoverDuration = 0.15f;

    private List<Renderer> _renderers = new List<Renderer>();
    private List<Material> _materials = new List<Material>();
    private List<Color> _originalColors = new List<Color>();
    private Sequence _flashSequence;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private void Awake()
    {
        CacheRenderers();
    }

    private void CacheRenderers()
    {
        _renderers.Clear();
        _materials.Clear();
        _originalColors.Clear();

        // 자신과 자식의 모든 Renderer 수집
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);

        foreach (var renderer in allRenderers)
        {
            if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
            {
                _renderers.Add(renderer);

                foreach (var mat in renderer.materials)
                {
                    _materials.Add(mat);
                    _originalColors.Add(GetMaterialColor(mat));
                }
            }
        }
    }

    private Color GetMaterialColor(Material mat)
    {
        if (mat.HasProperty(BaseColorId))
        {
            return mat.GetColor(BaseColorId);
        }
        if (mat.HasProperty(ColorId))
        {
            return mat.GetColor(ColorId);
        }
        return Color.white;
    }

    private void SetMaterialColor(Material mat, Color color)
    {
        if (mat.HasProperty(BaseColorId))
        {
            mat.SetColor(BaseColorId, color);
        }
        if (mat.HasProperty(ColorId))
        {
            mat.SetColor(ColorId, color);
        }
    }

    public void Flash()
    {
        if (_materials.Count == 0) return;

        // 기존 시퀀스 종료
        _flashSequence?.Kill();

        _flashSequence = DOTween.Sequence();

        // 빨간색으로
        _flashSequence.AppendCallback(() => SetAllColors(_hitColor));
        _flashSequence.AppendInterval(_hitDuration);

        // 흰색으로
        _flashSequence.AppendCallback(() => SetAllColors(_flashColor));
        _flashSequence.AppendInterval(_flashDuration);

        // 원래 색으로 복귀 (Tween으로 부드럽게)
        _flashSequence.Append(CreateColorTween(_recoverDuration));
    }

    private void SetAllColors(Color color)
    {
        foreach (var mat in _materials)
        {
            SetMaterialColor(mat, color);
        }
    }

    private Tween CreateColorTween(float duration)
    {
        float progress = 0f;
        return DOTween.To(() => progress, x =>
        {
            progress = x;
            for (int i = 0; i < _materials.Count; i++)
            {
                Color currentColor = Color.Lerp(_flashColor, _originalColors[i], progress);
                SetMaterialColor(_materials[i], currentColor);
            }
        }, 1f, duration).SetEase(Ease.OutQuad);
    }

    public void ResetColors()
    {
        _flashSequence?.Kill();

        for (int i = 0; i < _materials.Count; i++)
        {
            SetMaterialColor(_materials[i], _originalColors[i]);
        }
    }

    private void OnDestroy()
    {
        _flashSequence?.Kill();
    }
}
