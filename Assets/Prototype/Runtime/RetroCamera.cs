using UnityEngine;

namespace Patio
{
    [RequireComponent(typeof(Camera))]
    public sealed class RetroCamera : MonoBehaviour
    {
        public PhysicalMotor target;
        [Range(240, 960)] public int internalWidth = 480;
        public float distance = 5.2f;
        public float followSpeed = 12;
        private float yaw;
        private float pitch = 25;
        private Vector3 focus;
        private Vector3 previousMouse;
        private RenderTexture buffer;
        private Camera lens;
        private GUIStyle title;
        private GUIStyle text;
        private bool initialized;

        private void Awake()
        {
            lens = GetComponent<Camera>();
            previousMouse = Input.mousePosition;
        }

        private void LateUpdate()
        {
            if (!target) return;
            Vector3 mouse = Input.mousePosition;
            if (Input.GetMouseButton(1) && Input.GetMouseButtonDown(1)) previousMouse = mouse;
            if (Input.GetMouseButton(1))
            {
                yaw += (mouse.x - previousMouse.x) * .22f;
                pitch = Mathf.Clamp(pitch - (mouse.y - previousMouse.y) * .18f, 12, 65);
            }
            previousMouse = mouse;
            distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * .35f, 2.5f, 8);
            Vector3 desiredFocus = target.Body.Root.position + Vector3.up * .45f;
            if (!initialized || Vector3.Distance(focus, desiredFocus) > 10)
            { focus = desiredFocus; initialized = true; }
            focus = Vector3.Lerp(focus, desiredFocus, 1 - Mathf.Exp(-followSpeed * Time.deltaTime));
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 backward = rotation * Vector3.back;
            float safeDistance = distance;
            foreach (var hit in Physics.SphereCastAll(focus, .22f, backward, distance,
                ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform.IsChildOf(target.transform)) continue;
                safeDistance = Mathf.Min(safeDistance, Mathf.Max(.25f, hit.distance - .12f));
            }
            transform.SetPositionAndRotation(focus + backward * safeDistance, rotation);
            int height = Mathf.Max(1, Mathf.RoundToInt(internalWidth * (float)Screen.height / Mathf.Max(1, Screen.width)));
            if (!buffer || buffer.width != internalWidth || buffer.height != height)
            {
                ReleaseBuffer();
                buffer = new RenderTexture(internalWidth, height, 24)
                {
                    name = "Patio low resolution frame",
                    filterMode = FilterMode.Point,
                    antiAliasing = 1
                };
                buffer.Create();
                lens.targetTexture = buffer;
            }
        }

        private void OnGUI()
        {
            if (!target || !buffer) return;
            GUI.depth = 100;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), buffer, ScaleMode.StretchToFill, false);
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
                title.normal.textColor = new Color(.90f, .78f, .48f);
                text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                text.normal.textColor = new Color(.88f, .90f, .86f);
            }
            float width = Mathf.Min(490, Screen.width - 24);
            GUI.Box(new Rect(12, 12, width, 99), GUIContent.none);
            GUI.Label(new Rect(24, 18, width - 24, 32), "PATIO / PRUEBA 01", title);
            GUI.Label(new Rect(24, 51, width - 24, 54),
                "WASD mover · Shift correr · Espacio saltar\nC cubrirse · E subir · R caer · T reiniciar", text);
            GUI.Box(new Rect(12, Screen.height - 83, width, 71), GUIContent.none);
            GUI.Label(new Rect(24, Screen.height - 77, width - 24, 30),
                target.Body.Limp ? "Sin equilibrio · recuperando postura…" : target.Hint, text);
            GUI.Label(new Rect(24, Screen.height - 49, width - 24, 30),
                "Botón derecho: cámara · Rueda: distancia", text);
        }

        private void OnDisable() { ReleaseBuffer(); }

        private void ReleaseBuffer()
        {
            if (lens) lens.targetTexture = null;
            if (!buffer) return;
            buffer.Release();
            Destroy(buffer);
            buffer = null;
        }
    }
}
