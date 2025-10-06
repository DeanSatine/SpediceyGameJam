using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TMPTextDancer : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("How fast the animation cycles through each letter.")]
    public float speed = 5f;

    [Tooltip("How much each letter moves up and down.")]
    public float amplitude = 5f;

    [Tooltip("How much time offset between each letter's animation.")]
    public float waveOffset = 0.2f;

    [Tooltip("How much each letter rotates (in degrees).")]
    public float rotationAmount = 10f;

    [Tooltip("Enable color pulsing effect.")]
    public bool colorPulse = true;

    [Tooltip("Color change speed if color pulse is enabled.")]
    public float colorSpeed = 2f;

    private TMP_Text tmpText;
    private TMP_MeshInfo[] cachedMeshInfo;
    private Vector3[][] originalVertices;

    void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    void Start()
    {
        tmpText.ForceMeshUpdate();
        cachedMeshInfo = tmpText.textInfo.CopyMeshInfoVertexData();
    }

    void Update()
    {
        AnimateText();
    }

    void AnimateText()
    {
        tmpText.ForceMeshUpdate();
        var textInfo = tmpText.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // Base wave animation (vertical + rotation)
            float time = Time.time * speed + i * waveOffset;
            float offsetY = Mathf.Sin(time) * amplitude;
            float rotationZ = Mathf.Sin(time) * rotationAmount;

            // Center pivot
            Vector3 charMid = (vertices[vertexIndex] + vertices[vertexIndex + 2]) / 2;

            Matrix4x4 matrix = Matrix4x4.TRS(
                new Vector3(0, offsetY, 0),
                Quaternion.Euler(0, 0, rotationZ),
                Vector3.one
            );

            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j] -= charMid;
                vertices[vertexIndex + j] = matrix.MultiplyPoint3x4(vertices[vertexIndex + j]);
                vertices[vertexIndex + j] += charMid;
            }

            // Optional color pulsing
            if (colorPulse)
            {
                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
                byte alpha = (byte)(Mathf.Lerp(100, 255, (Mathf.Sin(time * colorSpeed) + 1f) / 2f));
                colors[vertexIndex + 0].a = alpha;
                colors[vertexIndex + 1].a = alpha;
                colors[vertexIndex + 2].a = alpha;
                colors[vertexIndex + 3].a = alpha;
            }
        }

        // Push updated vertex data back to mesh
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            tmpText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }
}
