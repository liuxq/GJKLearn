using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ConvexCreateWindow : EditorWindow
{
    ConvexCollection cc = null;
    bool debugShow = false;

    [MenuItem("Lxq/Test")]
    static void Init()
    {
        EditorWindow.GetWindow<ConvexCreateWindow>();
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();

        cc = EditorGUILayout.ObjectField("凸包文件：", cc, typeof(ConvexCollection)) as ConvexCollection;

        if (GUILayout.Button("创建Convexdatas"))
        {
            cc = ScriptableObject.CreateInstance<ConvexCollection>();
            AssetDatabase.CreateAsset(cc, "Assets/Resources/ConvexDataCollection.asset");
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("创建Convex"))
        {
            GameObject cur = Selection.activeGameObject;
            if(cur != null)
            {
                Mesh m = cur.GetComponent<MeshFilter>().sharedMesh;
                if(m != null)
                {
                    ConvexData cd = new ConvexData();
                    if (ConvexUtil.MakeHull(m, cur.transform, "", ref cd))
                    {
                        cc.ConvexDatas.Add(cd);
                    }
                }
            }
        }

        if (GUILayout.Button("从文件添加凸包"))
        {

            LEditTextFile tf = new LEditTextFile();
            tf.OpenRead("convex.txt");

            ConvexData cd = new ConvexData();
            cd.EditLoad(tf);
            cc.ConvexDatas.Add(cd);

            tf.Close();
        }

        if (GUILayout.Button("显示凸包"))
        {
            cc.DebugRender(debugShow);
            debugShow = !debugShow;
        }

        
    }


}