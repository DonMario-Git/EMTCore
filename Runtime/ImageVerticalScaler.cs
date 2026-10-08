using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Escala una Image de UGUI verticalmente para que coincida con la altura
/// de un TextMeshProUGUI o TMP_InputField objetivo.
/// El pivot Y del RectTransform de la Image debe estar en 1 (arriba),
/// de modo que el escalado ocurra de arriba hacia abajo.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Image))]
public class ImageVerticalScaler : MonoBehaviour
{
    public enum TargetType { TextMeshPro, InputField }

    [Header("Objetivo")]
    [Tooltip("Tipo de componente al que se escalará la imagen.")]
    public TargetType targetType = TargetType.TextMeshPro;

    [Tooltip("TextMeshProUGUI objetivo (si el tipo es TextMeshPro).")]
    public TextMeshProUGUI targetText;

    [Tooltip("TMP_InputField objetivo (si el tipo es InputField).")]
    public TMP_InputField targetInputField;

    [Header("Ajuste")]
    [Tooltip("Altura adicional (en píxeles) que se suma a la altura del objetivo.")]
    public float heightOffset = 0f;

    [Tooltip("Altura mínima permitida para la imagen (en píxeles).")]
    public float minHeight = 0f;

    [Tooltip("Altura máxima permitida (0 = sin límite).")]
    public float maxHeight = 0f;

    [Header("Pivot")]
    [Tooltip("Fuerza el pivot Y del RectTransform de la imagen a 1 automáticamente.")]
    public bool enforcePivotY1 = true;

    // ── Caché ─────────────────────────────────────────────────────────────────
    private RectTransform _imageRect;
    private RectTransform _targetRect;
    private float _lastTargetHeight = -1f;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake() => Initialize();
    private void OnEnable() => Initialize();

    private void Initialize()
    {
        _imageRect = GetComponent<RectTransform>();
        ResolveTarget();
    }

    private void ResolveTarget()
    {
        _targetRect = null;

        switch (targetType)
        {
            case TargetType.TextMeshPro:
                if (targetText != null)
                    _targetRect = targetText.GetComponent<RectTransform>();
                break;

            case TargetType.InputField:
                if (targetInputField != null)
                    _targetRect = targetInputField.GetComponent<RectTransform>();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_targetRect == null)
        {
            ResolveTarget();
            return;
        }

        float targetHeight = _targetRect.rect.height;

        // Solo actualiza si la altura cambió para evitar trabajo innecesario
        if (Mathf.Approximately(targetHeight, _lastTargetHeight)) return;

        ApplyScale(targetHeight);
        _lastTargetHeight = targetHeight;
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void ApplyScale(float targetHeight)
    {
        if (_imageRect == null) return;

        // Forzar pivot Y = 1 (arriba → abajo)
        if (enforcePivotY1)
        {
            Vector2 pivot = _imageRect.pivot;
            if (!Mathf.Approximately(pivot.y, 1f))
            {
                pivot.y = 1f;
                _imageRect.pivot = pivot;
            }
        }

        // Calcular nueva altura
        float newHeight = targetHeight + heightOffset;

        // Aplicar límites
        if (minHeight > 0f)
            newHeight = Mathf.Max(newHeight, minHeight);

        if (maxHeight > 0f)
            newHeight = Mathf.Min(newHeight, maxHeight);

        // Aplicar solo la altura (escala vertical), mantener ancho intacto
        Vector2 size = _imageRect.sizeDelta;
        size.y = newHeight;
        _imageRect.sizeDelta = size;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Fuerza una actualización inmediata desde código externo.
    /// </summary>
    public void ForceUpdate()
    {
        ResolveTarget();
        if (_targetRect != null)
        {
            ApplyScale(_targetRect.rect.height);
            _lastTargetHeight = _targetRect.rect.height;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Diferir la aplicación al siguiente frame del editor para evitar el error:
        // "SendMessage cannot be called during Awake, CheckConsistency, or OnValidate"
        UnityEditor.EditorApplication.delayCall += EditorDelayedApply;
    }

    private void EditorDelayedApply()
    {
        // Cancelar suscripción inmediatamente para no acumular llamadas
        UnityEditor.EditorApplication.delayCall -= EditorDelayedApply;

        // El objeto puede haber sido destruido entre el OnValidate y el delayCall
        if (this == null) return;

        Initialize();
        if (_targetRect != null)
            ApplyScale(_targetRect.rect.height);
    }
#endif
}