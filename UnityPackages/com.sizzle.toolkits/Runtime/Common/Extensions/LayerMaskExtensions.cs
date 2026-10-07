using UnityEngine;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// <see cref="LayerMask"/> 비트 연산 및 검사 편의 확장 메서드 모음입니다.
    /// </summary>
    public static class LayerMaskExtensions
    {
        /// <summary>
        /// 지정한 정수 레이어 인덱스가 레이어마스크에 포함되어 있는지 확인합니다.
        /// </summary>
        public static bool Contains(this LayerMask mask, int layerIndex)
        {
            return (mask.value & (1 << layerIndex)) != 0;
        }

        /// <summary>
        /// 지정한 게임오브젝트의 레이어가 레이어마스크에 포함되어 있는지 확인합니다.
        /// </summary>
        public static bool Contains(this LayerMask mask, GameObject gameObject)
        {
            if (gameObject == null) return false;
            return mask.Contains(gameObject.layer);
        }

        /// <summary>
        /// 지정한 컴포넌트가 속한 게임오브젝트의 레이어가 레이어마스크에 포함되어 있는지 확인합니다.
        /// </summary>
        public static bool Contains(this LayerMask mask, Component component)
        {
            if (component == null) return false;
            return mask.Contains(component.gameObject.layer);
        }

        /// <summary>
        /// 레이어마스크에 새로운 레이어 인덱스를 추가하여 반환합니다.
        /// </summary>
        public static LayerMask AddLayer(this LayerMask mask, int layerIndex)
        {
            return mask.value | (1 << layerIndex);
        }

        /// <summary>
        /// 레이어마스크에서 특정 레이어 인덱스를 제거하여 반환합니다.
        /// </summary>
        public static LayerMask RemoveLayer(this LayerMask mask, int layerIndex)
        {
            return mask.value & ~(1 << layerIndex);
        }
    }
}
