using System.Collections.Generic;
using UnityEngine;

namespace TokyoExpress.Grid
{
    /// <summary>
    /// Displays pop glowing direction arrows on intersections (Left, Right, Forward)
    /// to prompt the player to swipe when paused at a fork.
    /// </summary>
    public class IntersectionArrowsIndicator : MonoBehaviour
    {
        [Header("Prefabs & Colors")]
        [SerializeField] private Color arrowColor = new Color(1f, 0.45f, 0.45f, 0.95f);
        [SerializeField] private float pulseSpeed = 6f;
        [SerializeField] private float bounceHeight = 0.15f;

        private GameObject arrowForward;
        private GameObject arrowLeft;
        private GameObject arrowRight;

        private void Awake()
        {
            BuildProceduralArrows();
            HideAll();
        }

        private void BuildProceduralArrows()
        {
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader == null) urpShader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (urpShader == null) urpShader = Shader.Find("Standard");

            Material mat = new Material(urpShader);
            mat.color = arrowColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", arrowColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.8f);

            arrowForward = CreateArrowVisual("Arrow_Forward", Vector3.forward, mat);
            arrowLeft = CreateArrowVisual("Arrow_Left", Vector3.left, mat);
            arrowRight = CreateArrowVisual("Arrow_Right", Vector3.right, mat);
        }

        private GameObject CreateArrowVisual(string name, Vector3 direction, Material mat)
        {
            GameObject container = new GameObject(name);
            container.transform.SetParent(transform, false);

            // Stem of arrow
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stem.name = "Stem";
            stem.transform.SetParent(container.transform, false);
            stem.transform.localScale = new Vector3(0.2f, 0.08f, 0.5f);
            stem.transform.localPosition = new Vector3(0f, 0.08f, 0.25f);
            stem.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Destroy(stem.GetComponent<Collider>());

            // Head / Pointer triangle (represented as flat angled diamond)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "Head";
            head.transform.SetParent(container.transform, false);
            head.transform.localScale = new Vector3(0.38f, 0.085f, 0.38f);
            head.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            head.transform.localPosition = new Vector3(0f, 0.08f, 0.55f);
            head.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Destroy(head.GetComponent<Collider>());

            // Rotate container towards direction
            container.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            container.transform.position = transform.position + direction * 0.8f + Vector3.up * 0.05f;

            return container;
        }

        public void ShowArrows(List<Vector3> availableDirections, Vector3 currentHeading)
        {
            HideAll();

            foreach (var dir in availableDirections)
            {
                // Determine if it's forward, left or right relative to heading
                float angle = Vector3.SignedAngle(currentHeading, dir, Vector3.up);

                if (Mathf.Abs(angle) < 25f && arrowForward != null)
                {
                    arrowForward.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    arrowForward.transform.position = transform.position + dir * 0.85f + Vector3.up * 0.08f;
                    arrowForward.SetActive(true);
                }
                else if (angle < -25f && arrowLeft != null)
                {
                    arrowLeft.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    arrowLeft.transform.position = transform.position + dir * 0.85f + Vector3.up * 0.08f;
                    arrowLeft.SetActive(true);
                }
                else if (angle > 25f && arrowRight != null)
                {
                    arrowRight.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    arrowRight.transform.position = transform.position + dir * 0.85f + Vector3.up * 0.08f;
                    arrowRight.SetActive(true);
                }
            }
        }

        public void HideAll()
        {
            if (arrowForward != null) arrowForward.SetActive(false);
            if (arrowLeft != null) arrowLeft.SetActive(false);
            if (arrowRight != null) arrowRight.SetActive(false);
        }

        private void Update()
        {
            // Subtle pulse / bobbing effect
            float bob = Mathf.Sin(Time.time * pulseSpeed) * bounceHeight * 0.05f;
            if (arrowForward != null && arrowForward.activeSelf) arrowForward.transform.position += Vector3.up * bob;
            if (arrowLeft != null && arrowLeft.activeSelf) arrowLeft.transform.position += Vector3.up * bob;
            if (arrowRight != null && arrowRight.activeSelf) arrowRight.transform.position += Vector3.up * bob;
        }
    }
}
