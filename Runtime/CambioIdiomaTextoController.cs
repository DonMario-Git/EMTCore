using NaughtyAttributes;
using TMPro;
using UnityEngine;

namespace EMT.Core
{
    public class CambioIdiomaTextoController : MonoBehaviour
    {
        [ReadOnly] public TextMeshProUGUI texto;

        public string spanishText, englishText;

        private void OnValidate()
        {
            if (texto == null) texto = GetComponent<TextMeshProUGUI>();

            if (texto != null) spanishText = string.IsNullOrEmpty(spanishText) ? texto.text : spanishText;
        }

        private void Awake()
        {
            ActualizarIdioma();
        }

        public void ActualizarIdioma()
        {
            if (texto != null && !string.IsNullOrEmpty(englishText)) texto.text = Singleton<DataManager>.singleton.currentData.lenguageSettings.sellectedLenguage == Lenguage.SPANISH ? spanishText : englishText;
        }
    }
}