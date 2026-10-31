using UnityEngine;

namespace HybridMotion
{
    public sealed class MovementDebugHUD : MonoBehaviour
    {
        [SerializeField] private PlayerMotor player;
        public void Configure(PlayerMotor motor) { player = motor; }
        private void OnGUI()
        {
            if (!player) return;
            GUI.Box(new Rect(12, 12, Mathf.Min(580, Screen.width - 24), 100),
                "HYBRID MOTION / FASE 1\n" +
                "WASD mover · Shift correr · Ctrl caminar · Espacio saltar\n" +
                "Mouse cámara · Esc liberar/capturar cursor\n" +
                $"Estado: {player.State.Current} | Velocidad: {Vector3.ProjectOnPlane(player.Velocity, Vector3.up).magnitude:F1} m/s | Suelo: {player.Grounded}");
        }
    }
}
