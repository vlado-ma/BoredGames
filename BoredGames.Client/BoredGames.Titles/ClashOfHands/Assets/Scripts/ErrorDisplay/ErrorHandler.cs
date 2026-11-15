using Assets.Scripts.GamePlay;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.ErrorDisplay
{
    public class ErrorHandler : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _errorText;

        void Start()
        {
            _errorText.text = $"Error: {GameState.Instance.Error}";
        }
    }
}