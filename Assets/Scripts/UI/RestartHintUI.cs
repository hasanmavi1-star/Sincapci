using UnityEngine;
using TMPro;

namespace SquirrelGame.UI
{
    /// <summary>
    /// Ekranın sağ üst köşesinde "R = Restart" ipucunu gösterir.
    /// Canvas ve TextMeshPro bileşenleri ile çalışır.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class RestartHintUI : MonoBehaviour
    {
        [SerializeField] private string _hintText = "R  =  Yeniden Başlat";
        [SerializeField] private float  _fontSize  = 18f;
        [SerializeField] private Color  _color     = new Color(1f, 1f, 1f, 0.75f);

        private TextMeshProUGUI _label;

        private void Awake()
        {
            _label = GetComponent<TextMeshProUGUI>();
            Apply();
        }

        private void Apply()
        {
            _label.text      = _hintText;
            _label.fontSize  = _fontSize;
            _label.color     = _color;
            _label.alignment = TextAlignmentOptions.TopRight;
        }

#if UNITY_EDITOR
        private void OnValidate() { if (_label != null) Apply(); }
#endif
    }
}
