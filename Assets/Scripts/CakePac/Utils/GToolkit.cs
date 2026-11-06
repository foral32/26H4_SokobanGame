using UnityEngine;

/// <summary>
/// 工具箱
/// </summary>
public static class GToolkit
{
    #region 向量
    public static class Vector
    {
        public static Vector3 Vector2To3(Vector2 vec) => new Vector3(vec.x, vec.y, 0f);
        public static Vector2 Vector3To2(Vector3 vec) => new Vector2(vec.x, vec.y);

        /// <summary>
        /// 锚点转向量
        /// </summary>
        public static Vector2 AlignmentToVector2(SpriteAlignment alignment)
        {
            return alignment switch
            {
                SpriteAlignment.TopLeft => new Vector2(-1, -1),
                SpriteAlignment.TopCenter => new Vector2(0, -1),
                SpriteAlignment.TopRight => new Vector2(1, -1),
                SpriteAlignment.LeftCenter => new Vector2(-1, 0),
                SpriteAlignment.Center => new Vector2(0, 0),
                SpriteAlignment.RightCenter => new Vector2(1, 0),
                SpriteAlignment.BottomLeft => new Vector2(-1, 1),
                SpriteAlignment.BottomCenter => new Vector2(0, 1),
                SpriteAlignment.BottomRight => new Vector2(1, 1),
                _ => Vector2.zero
            };
        }

        /// <summary>
        /// 方向转向量
        /// </summary>
        public static Vector2 DirectionToVector2(Direction dir)
        {
            return dir switch
            {
                Direction.Up => Vector2.up,
                Direction.Down => Vector2.down,
                Direction.Left => Vector2.left,
                Direction.Right => Vector2.right,
                _ => Vector2.zero
            };
        }
        /// <summary>
        /// 由A的位置指向B的位置的向量
        /// </summary>
        public static Vector2 GetA2BDir(GameObject A, GameObject B, bool normalized = true)
        {
            if (A == null || B == null) return Vector2.zero;
            if (normalized)
                return (B.transform.position - A.transform.position).normalized;
            else
                return B.transform.position - A.transform.position;
        }
    }
    #endregion

    #region UI
    public static class UI
    {
        /// <summary>
        /// 获取UI相机
        /// </summary>
        public static Camera GetUICamera(RectTransform target)
        {
            Camera uiCam = null;
            Canvas canvas = target.GetComponentInParent<Canvas>();
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCam = canvas.worldCamera;

            return uiCam;
        }
    }
    #endregion

    #region 位置转换器
    public static class Pos
    {
        /// <summary>
        /// 世界坐标转屏幕坐标
        /// </summary>
        public static Vector2 WorldPosToScreenPos(Vector2 worldPos, RectTransform target)
        {
            // UI相机
            var uiCam = UI.GetUICamera(target);

            // 屏幕坐标
            Vector2 sp = RectTransformUtility.WorldToScreenPoint(uiCam, worldPos);

            return sp;
        }

        /// <summary>
        /// 屏幕坐标转UI坐标
        /// </summary>
        public static Vector2 ScreenPosToUIPos(Vector2 sp, RectTransform target)
        {
            // 父物体
            if (target.parent != null)
                target = target.parent as RectTransform;

            // UI相机
            var uiCam = UI.GetUICamera(target);

            // UI位置
            Vector2 lp = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                target,
                sp,
                uiCam,
                out lp
            );

            return lp;
        }

        /// <summary>
        /// 世界坐标转UI坐标
        /// </summary>
        public static Vector2 WorldPosToUIPos(Vector2 worldPos, RectTransform src, RectTransform target)
        {
            // 屏幕坐标
            Vector2 sp = WorldPosToScreenPos(worldPos, src);

            // UI坐标
            Vector2 lp = ScreenPosToUIPos(sp, target);
            return lp;
        }

        /// <summary>
        /// 获取鼠标UI位置
        /// </summary>
        public static Vector2 GetMouseUIPos(RectTransform target)
        {
            // 鼠标屏幕位置
            return ScreenPosToUIPos(Input.mousePosition, target);
        }
    }
    #endregion

    #region 颜色转换器
    public static class Color
    {
        public static UnityEngine.Color FadeWhite => ToTransparent(UnityEngine.Color.white);
        public static UnityEngine.Color FadeBlack => ToTransparent(UnityEngine.Color.black);

        /// <summary>
        /// 转透明
        /// </summary>
        public static UnityEngine.Color ToTransparent(UnityEngine.Color color)
        {
            color.a = 0f;
            return color;
        }

        /// <summary>
        /// 转不透明
        /// </summary>
        public static UnityEngine.Color ToOpacue(UnityEngine.Color color)
        {
            color.a = 1f;
            return color;
        }

        /// <summary>
        /// 设置透明度
        /// </summary>
        public static UnityEngine.Color SetAlpha(UnityEngine.Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
    #endregion
    /// <summary>
    /// 方向
    /// </summary>
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right,
        Center,
        Horizontal,
        Vertical,
    }
}
