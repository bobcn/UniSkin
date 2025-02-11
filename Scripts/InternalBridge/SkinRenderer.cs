using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniSkin
{
    public static class SkinRenderer
    {
        public static Action<EditorWindow> OnWindowDispose = editorWindow => { };

        private static readonly Dictionary<string, GUIStyle> _cachedOriginalStyles = new Dictionary<string, GUIStyle>();

        public static bool Register(EditorWindow editorWindow)
        {
            var title = editorWindow.titleContent.text;
            var visualElement = editorWindow.rootVisualElement;

            if (visualElement.parent is null) return false;
            if (visualElement.parent[0] is not IMGUIContainer) return false;

            var guiContainer = visualElement.parent[0] as IMGUIContainer;
            var originalGUIHandler = guiContainer.onGUIHandler;

            guiContainer.onGUIHandler = () =>
            {
                var skin = CachedSkin.Skin;
                if (skin.WindowStyles.TryGetValue(title, out var windowStyle))
                {
                    ApplySkin(skin, windowStyle);

                    DrawTexture(windowStyle.CustomBackgroundTextureId, guiContainer.contentRect, skin.Textures);
                    DrawTexture(windowStyle.CustomBackgroundTextureId2, guiContainer.contentRect, skin.Textures);
                }
                else
                {
                    RestoreOriginalSkin();
                }

                originalGUIHandler.Invoke();
            };

            return true;
        }

        private static void DrawTexture(string textureId, Rect contentRect, IReadOnlyDictionary<string, SerializableTexture2D> textures)
        {
            if (!string.IsNullOrEmpty(textureId))
            {
                if (!textures.TryGetValue(textureId, out var serializableTexture) || !serializableTexture.IsValid) return;

                GUI.DrawTexture(contentRect, serializableTexture.Texture, ScaleMode.ScaleAndCrop);
            }
        }

        private static void ApplySkin(Skin skin, WindowStyle windowStyle)
        {
            var originalStyles = windowStyle.ElementStyles
                .Select(x =>
                {
                    var (styleName, elementStyle) = x;
                    var currentSkin = GUISkinUtility.GetCurrent();
                    var style = currentSkin.FindStyle(styleName);
                    if (style is null)
                    {
                        return null;
                    }

                    var originalStyle = new GUIStyle(style);

                    style.fontSize = elementStyle.FontSize;
                    style.fontStyle = elementStyle.FontStyle;

                    foreach (var (stateType, state) in elementStyle.StyleStates)
                    {
                        GUIStyleState targetState = default;

                        switch (stateType)
                        {
                            case StyleStateType.Normal:
                                targetState = style.normal;
                                break;
                            case StyleStateType.Active:
                                targetState = style.active;
                                break;
                            case StyleStateType.Focused:
                                targetState = style.focused;
                                break;
                            case StyleStateType.Hover:
                                targetState = style.hover;
                                break;
                            case StyleStateType.OnNormal:
                                targetState = style.onNormal;
                                break;
                            case StyleStateType.OnActive:
                                targetState = style.onActive;
                                break;
                            case StyleStateType.OnFocused:
                                targetState = style.onFocused;
                                break;
                            case StyleStateType.OnHover:
                                targetState = style.onHover;
                                break;
                        }

                        targetState.textColor = state.TextColor;

                        if (state.BackgroundType is BackgroundType.Texture)
                        {
                            if (string.IsNullOrEmpty(state.BackgroundTextureId) || !skin.Textures.TryGetValue(state.BackgroundTextureId, out var serializableTexture))
                            {
                                targetState.background = null;
                            }
                            else
                            {
                                targetState.background = serializableTexture.Texture;
                            }
                        }
                        else if (state.BackgroundType is BackgroundType.Color)
                        {
                            targetState.background = ColorTexture.GetColorTexture(state.BackgroundColor);
                        }
                    }

                    if (!_cachedOriginalStyles.TryGetValue(styleName, out _))
                    {
                        _cachedOriginalStyles[styleName] = originalStyle;
                    }

                    return originalStyle;
                })
                .OfType<GUIStyle>()
                .ToArray();
        }

        private static void RestoreOriginalSkin()
        {
            foreach (var originalStyle in _cachedOriginalStyles.Values)
            {
                GUIStyle currentStyle = originalStyle.name;
                currentStyle.ApplyUniSkinStyle(originalStyle);
            }
        }
    }
}
