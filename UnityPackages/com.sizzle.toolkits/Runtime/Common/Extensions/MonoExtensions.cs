using System;
using UnityEngine;

namespace Sizzle.Toolkits
{
    /// <summary>
    /// GameObject, Component, Transform에 대한 컴포넌트 안전 획득, 자식 계층 검색/생성, 레이어 일괄 변경 등 유틸리티 확장 메서드를 제공합니다.
    /// </summary>
    public static class MonoExtensions
    {
        /// <summary>
        /// GameObject에서 지정한 컴포넌트(T)를 가져오거나, 없으면 새로 추가하여 반환합니다.
        /// </summary>
        /// <typeparam name="T">가져오거나 추가할 Component 타입</typeparam>
        /// <param name="gameObject">대상 GameObject</param>
        /// <returns>기존 또는 신규 생성된 Component</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null)
                return null;

            T component = gameObject.GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();
            return component;
        }

        /// <summary>
        /// Component가 부착된 GameObject에서 지정한 컴포넌트(T)를 가져오거나, 없으면 새로 추가하여 반환합니다.
        /// </summary>
        /// <typeparam name="T">가져오거나 추가할 Component 타입</typeparam>
        /// <param name="component">대상 Component</param>
        /// <returns>기존 또는 신규 생성된 Component</returns>
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return component != null ? component.gameObject.GetOrAddComponent<T>() : null;
        }

        /// <summary>
        /// 부모 Transform의 자식 중 지정한 이름의 Transform을 검색합니다.
        /// </summary>
        /// <param name="parent">검색 대상 부모 Transform</param>
        /// <param name="name">찾을 자식 오브젝트 이름</param>
        /// <param name="recursive">true일 경우 모든 하위 자식까지 재귀 검색하고, false일 경우 직계 자식만 검색합니다.</param>
        /// <returns>찾은 Transform (없으면 null)</returns>
        public static Transform SearchChild(this Transform parent, string name, bool recursive = true)
        {
            if (!parent)
                return null;

            if (!recursive)
                return parent.Find(name);

            Transform[] children = parent.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == name)
                    return child;
            }

            return null;
        }

        /// <summary>
        /// Transform의 로컬 위치(Vector3.zero), 로컬 회전(Quaternion.identity), 로컬 스케일(Vector3.one)을 초기화합니다.
        /// </summary>
        /// <param name="transform">대상 Transform</param>
        public static void ResetTransform(this Transform transform)
        {
            if (transform == null)
                return;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 부모 Transform 하위에 새로운 자식 GameObject를 생성하고 반환합니다. (로컬 Transform 초기화)
        /// </summary>
        /// <param name="parent">부모 Transform</param>
        /// <param name="name">생성할 자식 GameObject 이름</param>
        /// <returns>생성된 자식 GameObject</returns>
        public static GameObject CreateChildGameObject(this Transform parent, string name)
        {
            GameObject child = new GameObject(string.IsNullOrEmpty(name) ? "GameObject" : name);
            if (parent != null)
            {
                child.transform.SetParent(parent, false);
            }
            return child;
        }

        /// <summary>
        /// 경로(fullPath)를 따라 자식 GameObject를 찾거나, 존재하지 않는 계층 노드를 자동으로 생성하여 반환합니다.
        /// </summary>
        /// <param name="parent">시작 부모 Transform (null일 경우 씬 루트부터 시작)</param>
        /// <param name="fullPath">찾거나 생성할 오브젝트 경로 (예: "UI/Canvas/Panel")</param>
        /// <returns>찾았거나 생성된 최종 GameObject</returns>
        public static GameObject FindOrCreateGameObjectByPath(this Transform parent, string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                return parent != null ? parent.gameObject : null;

            if (parent != null)
            {
                Transform existing = parent.Find(fullPath);
                if (existing != null)
                    return existing.gameObject;
            }

            string[] parts = fullPath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return parent != null ? parent.gameObject : null;

            Transform current = parent;

            foreach (string part in parts)
            {
                if (current == null)
                {
                    GameObject rootObj = GameObject.Find("/" + part);
                    if (rootObj == null)
                    {
                        rootObj = GameObject.Find(part);
                        if (rootObj != null && rootObj.transform.parent != null)
                        {
                            rootObj = null;
                        }
                    }

                    if (rootObj == null)
                    {
                        rootObj = new GameObject(part);
                    }

                    current = rootObj.transform;
                }
                else
                {
                    Transform child = current.Find(part);
                    if (child == null)
                    {
                        GameObject newChild = current.CreateChildGameObject(part);
                        child = newChild.transform;
                    }

                    current = child;
                }
            }

            return current != null ? current.gameObject : null;
        }

        /// <summary>
        /// 지정한 GameObject와 모든 하위 자식 오브젝트의 레이어(Layer)를 재귀적으로 변경합니다.
        /// </summary>
        /// <param name="gameObject">대상 GameObject</param>
        /// <param name="layer">설정할 레이어 인덱스</param>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            if (gameObject == null)
                return;

            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                if (child != null)
                {
                    child.gameObject.SetLayerRecursively(layer);
                }
            }
        }

        /// <summary>
        /// 지정한 Transform과 모든 하위 자식 오브젝트의 레이어(Layer)를 재귀적으로 변경합니다.
        /// </summary>
        /// <param name="transform">대상 Transform</param>
        /// <param name="layer">설정할 레이어 인덱스</param>
        public static void SetLayerRecursively(this Transform transform, int layer)
        {
            if (transform != null)
            {
                transform.gameObject.SetLayerRecursively(layer);
            }
        }

        /// <summary>
        /// 지정한 GameObject와 모든 하위 자식 오브젝트의 레이어(Layer)를 레이어 이름을 통해 재귀적으로 변경합니다.
        /// </summary>
        /// <param name="gameObject">대상 GameObject</param>
        /// <param name="layerName">설정할 레이어 이름</param>
        public static void SetLayerRecursively(this GameObject gameObject, string layerName)
        {
            if (gameObject == null)
                return;

            int layer = LayerMask.NameToLayer(layerName);
            if (layer != -1)
            {
                gameObject.SetLayerRecursively(layer);
            }
            else
            {
                Debug.LogWarning($"[MonoExtensions] Layer '{layerName}' does not exist.");
            }
        }

        /// <summary>
        /// 지정한 Transform과 모든 하위 자식 오브젝트의 레이어(Layer)를 레이어 이름을 통해 재귀적으로 변경합니다.
        /// </summary>
        /// <param name="transform">대상 Transform</param>
        /// <param name="layerName">설정할 레이어 이름</param>
        public static void SetLayerRecursively(this Transform transform, string layerName)
        {
            if (transform != null)
            {
                transform.gameObject.SetLayerRecursively(layerName);
            }
        }
    }
}
