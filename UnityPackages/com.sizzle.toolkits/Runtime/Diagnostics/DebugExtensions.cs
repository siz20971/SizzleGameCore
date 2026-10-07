using UnityEngine;

namespace Sizzle.Toolkits.Diagnostics
{
    /// <summary>
    /// Debug.DrawLine을 기반으로 BoxCast, 3D 와이어프레임 박스, 부채꼴 및 원형 디버그 시각화를 제공하는 확장 유틸리티입니다.
    /// </summary>
	public static class DebugExtensions
	{
		/// <summary>
		/// BoxCast의 충돌 지점 위치에 충돌 박스를 렌더링합니다.
		/// </summary>
		public static void DrawBoxCastOnHit(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float hitInfoDistance, Color color)
		{
			origin = CastCenterOnCollision(origin, direction, hitInfoDistance);
			DrawBox(origin, halfExtents, orientation, color);
		}

		/// <summary>
		/// BoxCast의 시작 지점부터 목표 도달 거리까지의 이동 궤적 전체를 볼륨 박스로 렌더링합니다.
		/// </summary>
		public static void DrawBoxCastBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Vector3 direction, float distance, Color color)
		{
			direction.Normalize();
			Box bottomBox = new Box(origin, halfExtents, orientation);
			Box topBox = new Box(origin + (direction * distance), halfExtents, orientation);

			Debug.DrawLine(bottomBox.backBottomLeft, topBox.backBottomLeft, color);
			Debug.DrawLine(bottomBox.backBottomRight, topBox.backBottomRight, color);
			Debug.DrawLine(bottomBox.backTopLeft, topBox.backTopLeft, color);
			Debug.DrawLine(bottomBox.backTopRight, topBox.backTopRight, color);
			Debug.DrawLine(bottomBox.frontTopLeft, topBox.frontTopLeft, color);
			Debug.DrawLine(bottomBox.frontTopRight, topBox.frontTopRight, color);
			Debug.DrawLine(bottomBox.frontBottomLeft, topBox.frontBottomLeft, color);
			Debug.DrawLine(bottomBox.frontBottomRight, topBox.frontBottomRight, color);

			DrawBox(bottomBox, color);
			DrawBox(topBox, color);
		}

		/// <summary>
		/// 중심점, 절반 크기, 회전값을 기준으로 3D 와이어프레임 박스를 그립니다.
		/// </summary>
		public static void DrawBox(Vector3 origin, Vector3 halfExtents, Quaternion orientation, Color color, float duration = -1f)
		{
			DrawBox(new Box(origin, halfExtents, orientation), color, duration);
		}

		/// <summary>
		/// 정의된 Box 구조체의 12개 모서리 라인을 그립니다.
		/// </summary>
		public static void DrawBox(Box box, Color color, float duration = -1f)
		{
			Debug.DrawLine(box.frontTopLeft, box.frontTopRight, color, duration);
			Debug.DrawLine(box.frontTopRight, box.frontBottomRight, color, duration);
			Debug.DrawLine(box.frontBottomRight, box.frontBottomLeft, color, duration);
			Debug.DrawLine(box.frontBottomLeft, box.frontTopLeft, color, duration);

			Debug.DrawLine(box.backTopLeft, box.backTopRight, color, duration);
			Debug.DrawLine(box.backTopRight, box.backBottomRight, color, duration);
			Debug.DrawLine(box.backBottomRight, box.backBottomLeft, color, duration);
			Debug.DrawLine(box.backBottomLeft, box.backTopLeft, color, duration);

			Debug.DrawLine(box.frontTopLeft, box.backTopLeft, color, duration);
			Debug.DrawLine(box.frontTopRight, box.backTopRight, color, duration);
			Debug.DrawLine(box.frontBottomRight, box.backBottomRight, color, duration);
			Debug.DrawLine(box.frontBottomLeft, box.backBottomLeft, color, duration);
		}

		/// <summary>
		/// 3D 박스의 8개 꼭짓점 좌표를 로컬 및 월드 기준으로 계산하고 보관하는 구조체입니다.
		/// </summary>
		public struct Box
		{
			public Vector3 localFrontTopLeft { get; private set; }
			public Vector3 localFrontTopRight { get; private set; }
			public Vector3 localFrontBottomLeft { get; private set; }
			public Vector3 localFrontBottomRight { get; private set; }
			public Vector3 localBackTopLeft { get { return -localFrontBottomRight; } }
			public Vector3 localBackTopRight { get { return -localFrontBottomLeft; } }
			public Vector3 localBackBottomLeft { get { return -localFrontTopRight; } }
			public Vector3 localBackBottomRight { get { return -localFrontTopLeft; } }

			public Vector3 frontTopLeft { get { return localFrontTopLeft + origin; } }
			public Vector3 frontTopRight { get { return localFrontTopRight + origin; } }
			public Vector3 frontBottomLeft { get { return localFrontBottomLeft + origin; } }
			public Vector3 frontBottomRight { get { return localFrontBottomRight + origin; } }
			public Vector3 backTopLeft { get { return localBackTopLeft + origin; } }
			public Vector3 backTopRight { get { return localBackTopRight + origin; } }
			public Vector3 backBottomLeft { get { return localBackBottomLeft + origin; } }
			public Vector3 backBottomRight { get { return localBackBottomRight + origin; } }

			public Vector3 origin { get; private set; }

			public Box(Vector3 origin, Vector3 halfExtents, Quaternion orientation) : this(origin, halfExtents)
			{
				Rotate(orientation);
			}
			public Box(Vector3 origin, Vector3 halfExtents)
			{
				this.localFrontTopLeft = new Vector3(-halfExtents.x, halfExtents.y, -halfExtents.z);
				this.localFrontTopRight = new Vector3(halfExtents.x, halfExtents.y, -halfExtents.z);
				this.localFrontBottomLeft = new Vector3(-halfExtents.x, -halfExtents.y, -halfExtents.z);
				this.localFrontBottomRight = new Vector3(halfExtents.x, -halfExtents.y, -halfExtents.z);

				this.origin = origin;
			}


			public void Rotate(Quaternion orientation)
			{
				localFrontTopLeft = RotatePointAroundPivot(localFrontTopLeft, Vector3.zero, orientation);
				localFrontTopRight = RotatePointAroundPivot(localFrontTopRight, Vector3.zero, orientation);
				localFrontBottomLeft = RotatePointAroundPivot(localFrontBottomLeft, Vector3.zero, orientation);
				localFrontBottomRight = RotatePointAroundPivot(localFrontBottomRight, Vector3.zero, orientation);
			}
		}

		//This should work for all cast types
		static Vector3 CastCenterOnCollision(Vector3 origin, Vector3 direction, float hitInfoDistance)
		{
			return origin + (direction.normalized * hitInfoDistance);
		}

		static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
		{
			Vector3 direction = point - pivot;
			return pivot + rotation * direction;
		}


		/// <summary>
		/// 부채꼴 영역을 그립니다.
		/// </summary>
		public static void DrawSectorForm(Vector3 origin, Vector3 direction, float radius, float angle, Color color, int segments = 10, float duration = -1f)
		{
			Vector3 directionNormalized = direction.normalized;

			Debug.DrawLine(origin, origin + directionNormalized * radius, color, duration);

			Vector2 upperPoint = new Vector3(origin.x, origin.y) + (Quaternion.Euler(0, 0, angle / 2f) * directionNormalized).normalized * radius;
			Vector2 lowerPoint = new Vector3(origin.x, origin.y) + (Quaternion.Euler(0, 0, -angle / 2f) * directionNormalized).normalized * radius;
			Debug.DrawLine(origin, upperPoint, color, duration);
			Debug.DrawLine(origin, lowerPoint, color, duration);

			float angleStep = angle / segments;
			for (int i = 0; i <= segments; i++)
			{
				float currentAngle = -angle / 2f + angleStep * i;
				Vector3 point = Quaternion.Euler(0, 0, currentAngle) * directionNormalized * radius;
				Debug.DrawLine(origin, origin + point, color, duration);
			}

			// Draw the arc
			for (int i = 0; i < segments; i++)
			{
				float currentAngle = -angle / 2f + angleStep * i;
				Vector3 point1 = Quaternion.Euler(0, 0, currentAngle) * directionNormalized * radius;
				Vector3 point2 = Quaternion.Euler(0, 0, currentAngle + angleStep) * directionNormalized * radius;
				Debug.DrawLine(origin + point1, origin + point2, color, duration);
			}
		}

		/// <summary>
		/// 중심점과 반지름을 기준으로 2D/3D 원을 그립니다.
		/// </summary>
		public static void DrawCircle(Vector3 origin, float radius, Color color, int segments = 10, float duration = -1f)
		{
			DrawSectorForm(origin, Vector3.up, radius, 360f, color, segments, duration);
		}
	}
}