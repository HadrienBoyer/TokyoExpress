using DeBroglie.Rot;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tessera
{
    public enum FaceType
    {
        Point = 1,
        Edge = 2,
        Triangle = 3,
        Square = 4,
        Hex = 6,
    }

    /// <summary>
    /// Records the painted colors for a single face of one cube in a <see cref="TesseraTile"/>
    /// </summary>
    [Serializable]
    public class FaceDetails
    {
        public FaceType faceType;

        public int topLeft;
        public int top;
        public int topRight;
        public int left;
        public int center;
        public int right;
        public int bottomLeft;
        public int bottom;
        public int bottomRight;

        // So far, these are only used at runtime
        [NonSerialized]
        public int meshEdgeHash1;
        [NonSerialized]
        public int meshEdgeHash2;

        // For use with hexes
        public int hexRight { get { return right; } set { right = value; } }
        public int hexRightAndTopRight;
        public int hexTopRight { get { return topRight; } set { topRight = value; } }
        public int hexTopRightAndTopLeft { get { return top; } set { top = value; } }
        public int hexTopLeft { get { return topLeft; } set { topLeft = value; } }
        public int hexTopLeftAndLeft;
        public int hexLeft { get { return left; } set { left = value; } }
        public int hexLeftAndBottomLeft;
        public int hexBottomLeft { get { return bottomLeft; } set { bottomLeft = value; } }
        public int hexBottomLeftAndBottomRight { get { return bottom; } set { bottom = value; } }
        public int hexBottomRight { get { return bottomRight; } set { bottomRight = value; } }
        public int hexBottomRightAndRight;
        public int hexCenter { get { return center; } set { center = value; } }

        // TODO: These mutating methods should not be needed publically
        internal FaceDetails Clone()
        {
            return (FaceDetails)MemberwiseClone();
        }

        internal void ReflectX()
        {
            (topLeft, topRight) = (topRight, topLeft);
            (left, right) = (right, left);
            (bottomLeft, bottomRight) = (bottomRight, bottomLeft);
        }

        internal void ReflectY()
        {
            (bottomRight, topRight) = (topRight, bottomRight);
            (bottom, top) = (top, bottom);
            (bottomLeft, topLeft) = (topLeft, bottomLeft);
        }

        internal void RotateCw()
        {
            (topRight, bottomRight, bottomLeft, topLeft) = (topLeft, topRight, bottomRight, bottomLeft);
            (right, bottom, left, top) = (top, right, bottom, left);
        }

        internal void RotateCcw()
        {
            (topLeft, topRight, bottomRight, bottomLeft) = (topRight, bottomRight, bottomLeft, topLeft);
            (top, right, bottom, left) = (right, bottom, left, top);
        }

        internal void HexReflectX()
        {
            (hexRight, hexLeft) = (hexLeft, hexRight);
            (hexRightAndTopRight, hexTopLeftAndLeft) = (hexTopLeftAndLeft, hexRightAndTopRight);
            (hexTopRight, hexTopLeft) = (hexTopLeft, hexTopRight);
            (hexLeftAndBottomLeft, hexBottomRightAndRight) = (hexBottomRightAndRight, hexLeftAndBottomLeft);
            (hexBottomLeft, hexBottomRight) = (hexBottomRight, hexBottomLeft);
        }

        internal void HexReflectY()
        {
            (hexBottomLeft, hexTopLeft) = (hexTopLeft, hexBottomLeft);
            (hexLeftAndBottomLeft, hexTopLeftAndLeft) = (hexTopLeftAndLeft, hexLeftAndBottomLeft);
            (hexBottomRight, hexTopRight) = (hexTopRight, hexBottomRight);
            (hexBottomRightAndRight, hexRightAndTopRight) = (hexRightAndTopRight, hexBottomRightAndRight);
            (hexBottomLeftAndBottomRight, hexTopRightAndTopLeft) = (hexTopRightAndTopLeft, hexBottomLeftAndBottomRight);
        }

        internal void HexRotateCcw()
        {
            (hexTopRight, hexTopLeft, hexLeft, hexBottomLeft, hexBottomRight, hexRight) = (hexRight, hexTopRight, hexTopLeft, hexLeft, hexBottomLeft, hexBottomRight);
            (hexTopRightAndTopLeft, hexTopLeftAndLeft, hexLeftAndBottomLeft, hexBottomLeftAndBottomRight, hexBottomRightAndRight, hexRightAndTopRight) = (hexRightAndTopRight, hexTopRightAndTopLeft, hexTopLeftAndLeft, hexLeftAndBottomLeft, hexBottomLeftAndBottomRight, hexBottomRightAndRight);
        }

        internal void HexRotateCw()
        {
            (hexRight, hexTopRight, hexTopLeft, hexLeft, hexBottomLeft, hexBottomRight) = (hexTopRight, hexTopLeft, hexLeft, hexBottomLeft, hexBottomRight, hexRight);
            (hexRightAndTopRight, hexTopRightAndTopLeft, hexTopLeftAndLeft, hexLeftAndBottomLeft, hexBottomLeftAndBottomRight, hexBottomRightAndRight) = (hexTopRightAndTopLeft, hexTopLeftAndLeft, hexLeftAndBottomLeft, hexBottomLeftAndBottomRight, hexBottomRightAndRight, hexRightAndTopRight);
        }

        internal void TriangleReflectX()
        {
            (topLeft, topRight) = (topRight, topLeft);
            (bottomLeft, bottomRight) = (bottomRight, bottomLeft);
        }
        internal void TriangleReflectY()
        {
            (top, bottom) = (bottom, top);
            (topRight, bottomRight) = (bottomRight, topRight);
            (topLeft, bottomLeft) = (bottomLeft, topLeft);
        }

        // Corresponds to (CellRotation)~0.
        internal void TriangleReflectAskew()
        {
            (top, bottomLeft) = (bottomLeft, top);
            (topRight, bottom) = (bottom, topRight);
        }
        internal void TriangleRotateCcw()
        {
            (bottom, topRight, topLeft) = (topLeft, bottom, topRight);
            (top, bottomLeft, bottomRight) = (bottomRight, top, bottomLeft);
        }

        internal void TriangleRotateCcw60()
        {
            (bottomLeft, topLeft, top, topRight, bottomRight, bottom) = (topLeft, top, topRight, bottomRight, bottom, bottomLeft);
        }

        internal void TriangleRotateCw60()
        {
            (topLeft, top, topRight, bottomRight, bottom, bottomLeft) = (bottomLeft, topLeft, top, topRight, bottomRight, bottom);
        }

        public int this[SubFaceTuple subFace]
        {
            get
            {
                switch (subFace.type)
                {
                    case SubFaceType.Center:
                        return center;
                    case SubFaceType.Dir:
                        switch (faceType)
                        {
                            case FaceType.Square:
                                switch (subFace.value)
                                {
                                    case 0: return right;
                                    case 1: return top;
                                    case 2: return left;
                                    case 3: return bottom;
                                }
                                break;
                            case FaceType.Hex:
                                switch (subFace.value)
                                {
                                    case 0: return hexRight;
                                    case 1: return hexTopRight;
                                    case 2: return hexTopLeft;
                                    case 3: return hexLeft;
                                    case 4: return hexBottomLeft;
                                    case 5: return hexBottomRight;

                                }
                                break;
                        }
                        break;
                    case SubFaceType.Corner:
                        switch (faceType)
                        {
                            case FaceType.Square:
                                switch (subFace.value)
                                {
                                    case 0: return bottomRight;
                                    case 1: return topRight;
                                    case 2: return topLeft;
                                    case 3: return bottomLeft;
                                }
                                break;
                            case FaceType.Hex:
                                switch (subFace.value)
                                {
                                    case 0: return hexBottomRightAndRight;
                                    case 1: return hexRightAndTopRight;
                                    case 2: return hexTopRightAndTopLeft;
                                    case 3: return hexTopLeftAndLeft;
                                    case 4: return hexLeftAndBottomLeft;
                                    case 5: return hexBottomLeftAndBottomRight;
                                }
                                break;
                        }
                        break;
                }
                throw new Exception();
            }

            set
            {
                switch (subFace.type)
                {
                    case SubFaceType.Center:
                        center = value; return;
                    case SubFaceType.Dir:
                        switch (faceType)
                        {
                            case FaceType.Square:
                                switch (subFace.value)
                                {
                                    case 0: right = value; return;
                                    case 1: top = value; return;
                                    case 2: left = value; return;
                                    case 3: bottom = value; return;
                                }
                                break;
                            case FaceType.Hex:
                                switch (subFace.value)
                                {
                                    case 0: hexRight = value; return;
                                    case 1: hexTopRight = value; return;
                                    case 2: hexTopLeft = value; return;
                                    case 3: hexLeft = value; return;
                                    case 4: hexBottomLeft = value; return;
                                    case 5: hexBottomRight = value; return;

                                }
                                break;
                        }
                        break;
                    case SubFaceType.Corner:
                        switch (faceType)
                        {
                            case FaceType.Square:
                                switch (subFace.value)
                                {
                                    case 0: bottomRight = value; return;
                                    case 1: topRight = value; return;
                                    case 2: topLeft = value; return;
                                    case 3: bottomLeft = value; return;
                                }
                                break;
                            case FaceType.Hex:
                                switch (subFace.value)
                                {
                                    case 0: hexBottomRightAndRight = value; return;
                                    case 1: hexRightAndTopRight = value; return;
                                    case 2: hexTopRightAndTopLeft = value; return;
                                    case 3: hexTopLeftAndLeft = value; return;
                                    case 4: hexLeftAndBottomLeft = value; return;
                                    case 5: hexBottomLeftAndBottomRight = value; return;
                                }
                                break;
                        }
                        break;
                }
                throw new Exception();
            }
        }

        public override string ToString()
        {
            switch(faceType)
            {
                case FaceType.Point:
                    return $"({center})";
                case FaceType.Triangle:
                    return $"({topLeft},{top},{topRight};{center};{bottomLeft},{bottom},{bottomRight})";
                case FaceType.Hex:
                    return $"(right={hexRight}, {hexRightAndTopRight}, topRight={hexTopRight}, {hexTopRightAndTopLeft}, topLeft={hexTopLeft}, {hexTopLeftAndLeft}, left={hexLeft}, {hexLeftAndBottomLeft}, bottomLeft={hexBottomLeft}, {hexBottomLeftAndBottomRight}, bottomRight={hexBottomRight}, {hexBottomRightAndRight}, center={hexCenter}, )";
                case FaceType.Square:
                default:
                    return $"({topLeft},{top},{topRight};{left},{center},{right};{bottomLeft},{bottom},{bottomRight})";
            }
        }

        /// <summary>
        /// Checks if two FaceDetails have the same values.
        /// This is an exact match, with no reflection built in.
        /// See TesseraPalette.Match for a fuzzier match.
        /// </summary>
        public bool IsEquivalent(FaceDetails other)
        {
            return 
                topLeft == other.topLeft &&
                top == other.top &&
                topRight == other.topRight &&
                left == other.left &&
                center == other.center &&
                right == other.right &&
                bottomLeft == other.bottomLeft &&
                bottom == other.bottom &&
                bottomRight == other.bottomRight &&
                hexRightAndTopRight == other.hexRightAndTopRight &&
                hexTopLeftAndLeft == other.hexTopLeftAndLeft &&
                hexLeftAndBottomLeft == other.hexLeftAndBottomLeft &&
                hexBottomRightAndRight == other.hexBottomRightAndRight;
        }
    }
}