#if UNITY_EDITOR
using UnityEngine;

namespace Treemap
{
    internal interface ITreemapRenderable
    {
        Color GetColor();
        Rect GetPosition();
        string GetLabel();
    }
}
#endif