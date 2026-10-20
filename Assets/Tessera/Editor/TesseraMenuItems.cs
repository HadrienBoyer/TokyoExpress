using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    public static class TesseraMenuItems
    {

        [MenuItem("Tools/Tessera/Paint Tile From Mesh...")]
        public static void PaintTileFromMesh()
        {
            var window = ScriptableObject.CreateInstance<PaintTileFromMeshWindow>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 250, 150);
            window.Show();
        }
    }
}
