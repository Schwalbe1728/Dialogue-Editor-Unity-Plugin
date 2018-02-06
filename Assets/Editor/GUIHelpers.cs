using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GUIHelpers
{
    #region GUILayout.BeginArea
    public static void GUIArea(Rect screenRect, System.Action content)
    {
        GUILayout.BeginArea(screenRect);
        {
            content.Invoke();
        }
        GUILayout.EndArea();
    }

    public static void GUIArea(Rect screenRect, GUIStyle style, System.Action content)
    {
        GUILayout.BeginArea(screenRect, style);
        {
            content.Invoke();
        }
        GUILayout.EndArea();                       
    }
    #endregion    
    #region Horizontal
    #region GUILayout.BeginHorizontal
    public static void GUIHorizontal(System.Action content, params GUILayoutOption[] options)
    {
        GUILayout.BeginHorizontal(options);
        {
            content.Invoke();
        }
        GUILayout.EndHorizontal();
    }

    public static void GUIHorizontal(GUIStyle style, System.Action content, params GUILayoutOption[] options)
    {
        GUILayout.BeginHorizontal(style, options);
        {
            content.Invoke();
        }
        GUILayout.EndHorizontal();
    }
    #endregion
    #region EditorGUILayout.BeginHorizontal
    public static Rect EditorGUIHorizontal(System.Action content, params GUILayoutOption[] options)
    {
        Rect result = EditorGUILayout.BeginHorizontal(options);
        {
            content.Invoke();
        }
        EditorGUILayout.EndHorizontal();

        return result;
    }

    public static Rect EditorGUIHorizontal(GUIStyle style, System.Action content, params GUILayoutOption[] options)
    {
        Rect result = EditorGUILayout.BeginHorizontal(style, options);
        {
            content.Invoke();
        }
        EditorGUILayout.EndHorizontal();

        return result;
    }
    #endregion
    #endregion
    #region ScrollView
    #region GUILayout.ScrollView
    public static Vector2 GUIScrollView(Vector2 scrollPosition, System.Action content, params GUILayoutOption[] options)
    {
        Vector2 result = GUILayout.BeginScrollView(scrollPosition, options);
        {
            content.Invoke();
        }
        GUILayout.EndScrollView();        
        return result;
    }

    public static Vector2 GUIScrollView(Vector2 scrollPosition, GUIStyle style, System.Action content, params GUILayoutOption[] options)
    {
        Vector2 result = GUILayout.BeginScrollView(scrollPosition, style, options);
        {
            content.Invoke();
        }
        GUILayout.EndScrollView();

        return result;
    }
    #endregion
    #region EditorGUILayout.ScrollView
    public static Vector2 EditorGUIScrollView(Vector2 scrollPosition, System.Action content, params GUILayoutOption[] options)
    {
        Vector2 result = EditorGUILayout.BeginScrollView(scrollPosition, options);
        {
            content.Invoke();
        }
        EditorGUILayout.EndScrollView();
        return result;
    }

    public static Vector2 EditorGUIScrollView(Vector2 scrollPosition, GUIStyle style, System.Action content, params GUILayoutOption[] options)
    {
        Vector2 result = EditorGUILayout.BeginScrollView(scrollPosition, style, options);
        {
            content.Invoke();
        }
        EditorGUILayout.EndScrollView();        

        return result;
    }

    // TODO: Pozostałe EditorGUILayout.BeginScrollView, bo jest ich 5

    #endregion
    #endregion
}
