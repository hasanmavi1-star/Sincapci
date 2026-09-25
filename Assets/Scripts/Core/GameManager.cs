using UnityEngine;
using UnityEngine.SceneManagement;

namespace SquirrelGame.Core
{
    /// <summary>
    /// Oyunun genel akışını yönetir.
    /// • Awake'te hedef kare hızını ve VSync'i ayarlar.
    /// • R tuşu veya gamepad Start ile sahneyi yeniden başlatır.
    /// • UI butonuna bağlanmak için RestartGame() public metodu kullanılabilir.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Performance")]
        [Tooltip("Hedef kare hızı. -1 = sınırsız (VSync'e bağlı). Tavsiye: 60.")]
        [SerializeField] private int _targetFrameRate = 60;
        [Tooltip("0 = VSync kapalı (targetFrameRate geçerli), 1 = ekran yenileme hızına kilitle.")]
        [SerializeField] private int _vSyncCount = 0;

        [Header("Restart Settings")]
        [Tooltip("Yeniden başlatmak için kullanılacak klavye tuşu.")]
        [SerializeField] private KeyCode _restartKey = KeyCode.R;

        [Tooltip("Yeniden başlatma aktif olmadan önce beklenecek minimum süre (sn). Victory sonrası kazayla basılmasını önler.")]
        [SerializeField] private float _restartCooldown = 1.5f;

        private float _timeAlive;

        private void Awake()
        {
            // Kare hızını sabitle — düzensiz deltaTime kasmanın en yaygın sebebidir
            QualitySettings.vSyncCount  = _vSyncCount;
            Application.targetFrameRate = _targetFrameRate;
        }

        private void Update()
        {
            _timeAlive += Time.deltaTime;

            if (_timeAlive < _restartCooldown) return;

            bool restartPressed = false;

#if ENABLE_INPUT_SYSTEM
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var gamepad  = UnityEngine.InputSystem.Gamepad.current;

            if (keyboard != null && keyboard[UnityEngine.InputSystem.Key.R].wasPressedThisFrame)
                restartPressed = true;
            if (gamepad != null && gamepad.startButton.wasPressedThisFrame)
                restartPressed = true;
#else
            if (Input.GetKeyDown(_restartKey))
                restartPressed = true;
            if (Input.GetKeyDown(KeyCode.Joystick1Button7)) // Start / Options
                restartPressed = true;
#endif

            if (restartPressed)
                RestartGame();
        }

        /// <summary>
        /// Mevcut sahneyi sıfırdan yükler. UI butonuna da bağlanabilir.
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1f; // dondurulmuş kaldıysa sıfırla
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
