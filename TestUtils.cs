#if(UNITY_EDITOR)
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TestUtils
{
    public static SceneConstructor CreateScene()
    {
        return new SceneConstructor();
    }
}

public class SceneConstructor
{
    private Scene _scene;

    public SceneConstructor()
    {
        _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        EditorSceneManager.SetActiveScene(_scene);
    }

    public SceneConstructor WithName(string name)
    {
        _scene.name = name;
        return this;
    }

    public SceneConstructor WithGameObject<T>(string gObjName) where T : Component
    {
        GameObject gObj = new GameObject();
        gObj.name = gObjName;
        gObj.AddComponent<T>();
        return this;
    }

    public Scene Construct()
    {
        return _scene;
    }
}
#endif