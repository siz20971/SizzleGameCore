using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// <see cref="Transform"/>에 대한 좌표, 회전, 스케일 및 자식 제어 편의 확장 메서드 모음입니다.
    /// </summary>
    public static class TransformExtensions
    {
        #region Position Setters
        /// <summary>월드 좌표의 X값을 변경합니다.</summary>
        public static void SetPositionX(this Transform transform, float x)
        {
            Vector3 pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }

        /// <summary>월드 좌표의 Y값을 변경합니다.</summary>
        public static void SetPositionY(this Transform transform, float y)
        {
            Vector3 pos = transform.position;
            pos.y = y;
            transform.position = pos;
        }

        /// <summary>월드 좌표의 Z값을 변경합니다.</summary>
        public static void SetPositionZ(this Transform transform, float z)
        {
            Vector3 pos = transform.position;
            pos.z = z;
            transform.position = pos;
        }

        /// <summary>월드 좌표의 XY값을 변경합니다.</summary>
        public static void SetPositionXY(this Transform transform, float x, float y)
        {
            Vector3 pos = transform.position;
            pos.x = x;
            pos.y = y;
            transform.position = pos;
        }

        /// <summary>로컬 좌표의 X값을 변경합니다.</summary>
        public static void SetLocalPositionX(this Transform transform, float x)
        {
            Vector3 pos = transform.localPosition;
            pos.x = x;
            transform.localPosition = pos;
        }

        /// <summary>로컬 좌표의 Y값을 변경합니다.</summary>
        public static void SetLocalPositionY(this Transform transform, float y)
        {
            Vector3 pos = transform.localPosition;
            pos.y = y;
            transform.localPosition = pos;
        }

        /// <summary>로컬 좌표의 Z값을 변경합니다.</summary>
        public static void SetLocalPositionZ(this Transform transform, float z)
        {
            Vector3 pos = transform.localPosition;
            pos.z = z;
            transform.localPosition = pos;
        }
        #endregion

        #region Scale Setters
        /// <summary>로컬 스케일의 X값을 변경합니다.</summary>
        public static void SetLocalScaleX(this Transform transform, float x)
        {
            Vector3 scale = transform.localScale;
            scale.x = x;
            transform.localScale = scale;
        }

        /// <summary>로컬 스케일의 Y값을 변경합니다.</summary>
        public static void SetLocalScaleY(this Transform transform, float y)
        {
            Vector3 scale = transform.localScale;
            scale.y = y;
            transform.localScale = scale;
        }

        /// <summary>로컬 스케일의 Z값을 변경합니다.</summary>
        public static void SetLocalScaleZ(this Transform transform, float z)
        {
            Vector3 scale = transform.localScale;
            scale.z = z;
            transform.localScale = scale;
        }

        /// <summary>모든 축의 로컬 스케일을 균일한(Uniform) 값으로 설정합니다.</summary>
        public static void SetUniformScale(this Transform transform, float scale)
        {
            transform.localScale = new Vector3(scale, scale, scale);
        }
        #endregion

        #region Reset & Utility
        /// <summary>로컬 위치, 로컬 회전, 로컬 스케일을 기본값(0, identity, 1)으로 리셋합니다.</summary>
        public static void ResetLocal(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>하위의 모든 자식 트랜스폼들을 즉시 파괴합니다.</summary>
        /// <param name="transform">부모 트랜스폼</param>
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        /// <summary>직계 자식 트랜스폼 목록을 배열로 가져옵니다.</summary>
        public static List<Transform> GetChildren(this Transform transform)
        {
            int count = transform.childCount;
            var list = new List<Transform>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(transform.GetChild(i));
            }
            return list;
        }
        #endregion
    }
}
