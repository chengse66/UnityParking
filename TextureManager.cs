using UnityEngine;
using System.IO;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using System.Linq;

/// <summary>
/// 材质管理器 By https://github.com/ww3c
/// </summary>
public class TextureManager : MonoBehaviour {
    public string streaming_relpath;
    public bool initDirectory = true;
    public int numThread = 4;
    public ProgressHandle Progress;

    private TextureLoadObject loadObj;
    private bool runable = true;
    private List<TextureObject> loadList;
    private List<TextureObject> texList;
    private Dictionary<string, Texture2D> texMap;

    private Regex R_FileName = new Regex(@"[\w\-_]+(?=\.\w+$)", RegexOptions.Singleline);
    private Regex R_Texture = new Regex("(jpg|png|dds|tga)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
	void Start () {
        loadObj = default(TextureLoadObject);
        loadList = new List<TextureObject>();
        texList = new List<TextureObject>();
        texMap = new Dictionary<string, Texture2D>();

        loadObj.index = loadObj.total = 0;
        loadObj.loader = this;
        for (int i = 0; i < numThread; i++) StartCoroutine(Load_Texture());
        LoadDirectory(Path.Combine(Application.streamingAssetsPath, streaming_relpath));
	}

    /// <summary>
    /// 加载文件夹
    /// </summary>
    /// <param name="filename"></param>
    public void LoadDirectory(string filename){
        foreach (string file in Directory.GetFiles(filename)) {
            if (R_Texture.IsMatch(file)) {
                this.Load("file://"+file.Replace('\\','/'));
            }
        }
    }

    /// <summary>
    /// 加载图片文件
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="tag"></param>
    public void Load(string filename,string tag) {
        loadObj.total++;
        loadList.Add(new TextureObject() { 
            filename=filename,
            tag=tag
        });
        Debug.Log(filename);
    }

    /// <summary>
    /// 加载图片文件
    /// </summary>
    /// <param name="filename"></param>
    public void Load(string filename) {
        Match m= R_FileName.Match(filename);
        if (m != null) {
            Load(filename,m.Value.ToLower());
        }
    }

    /// <summary>
    /// 材质列表
    /// </summary>
    public List<TextureObject> textureList {
        get {
            return this.texList;
        }
    }

    /// <summary>
    /// 可用的无序列材质
    /// </summary>
    public List<Texture2D> textures {
        get {
            return (from m in texList where m.texture != null select m.texture).ToList<Texture2D>();
        }
    }

    IEnumerator Load_Texture() {
        TextureObject obj;
        while (runable)
        {
            if (loadList.Count > 0) {
                obj = loadList[0];
                loadList.Remove(obj);
                texList.Add(obj);
                using (WWW www = new WWW(obj.filename)) {
                    yield return www;
                    if (www.isDone) {
                        loadObj.index++;
                        if (string.IsNullOrEmpty(www.error))
                        {
                            //success
                            obj.texture=www.texture;
                            obj.success=true;
                        }
                        else { 
                            //error
                            obj.success=false;
                        }
                    }
                    loadObj.complete = loadObj.index == loadObj.total;
                    loadObj.current = obj;
                    texMap[obj.tag] = obj.texture;
                    Progress.Invoke(loadObj);
                }
            }
            yield return new WaitWhile(() => loadList.Count == 0);
        }
    }

    /// <summary>
    /// 调试输出
    /// </summary>
    /// <param name="obj"></param>
    public void DebugLoader(TextureLoadObject obj){
        StringBuilder sb=new StringBuilder();
        sb.AppendFormat("{0}/{1} {2}",obj.index,obj.total,obj.complete);
        sb.AppendLine();
        sb.AppendFormat("{0}",obj.current.filename);
        sb.AppendLine();
        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Destory
    /// </summary>
    void OnDestory() {
        foreach (KeyValuePair<string, Texture2D> m in texMap) Destroy(m.Value);
        texMap.Clear();
        loadList.Clear();
        texList.Clear();
    }

    /// <summary>
    /// 加载对象
    /// </summary>
    public struct TextureObject
    {
        public string tag;
        public string filename;
        public Texture2D texture;
        public bool success;
    }

    /// <summary>
    /// 加载的回调对象
    /// </summary>
    public struct TextureLoadObject {
        public TextureManager loader;
        public int index;
        public int total;
        public TextureObject current;
        public bool complete;
    }

    [System.Serializable]
    public class ProgressHandle : UnityEngine.Events.UnityEvent<TextureLoadObject> { }
}
