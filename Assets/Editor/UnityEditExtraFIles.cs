using UnityEditor;
using UnityEngine;

public class UnityEditExtraFIles
{
    // ---------------------------------------------------------
    // 1. 普通脚本 (保持 Unity 默认模样)
    // ---------------------------------------------------------
    [MenuItem("Assets/Create/C# 脚本/1. 普通脚本", false, 1)]
    public static void CreateNormalScript()
    {
        string templatePath = CreateTempTemplateFile("NormalTemplate.txt", GetNormalTemplate());
        // 调用 Unity 内置的模板生成工具
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewBehaviourScript.cs");
    }

    // ---------------------------------------------------------
    // 2. 资源配置脚本 (ScriptableObject)
    // ---------------------------------------------------------
    [MenuItem("Assets/Create/C# 脚本/2. 资源配置脚本", false, 2)]
    public static void CreateSOScript()
    {
        string templatePath = CreateTempTemplateFile("SOTemplate.txt", GetSOTemplate());
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewData.cs");
    }


    // ---------------------------------------------------------
    // 3. 空白配置脚本 
    // ---------------------------------------------------------
    [MenuItem("Assets/Create/C# 脚本/3. 空白配置脚本", false, 3)]
    public static void CreatNullScript()
    {
        string templatePath = CreateTempTemplateFile("NullTemplate.txt", GetNullTemplate());
        ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NUll.cs");

    }

    // =========================================================
    // 辅助方法：在后台自动生成模板文件
    // =========================================================
    private static string CreateTempTemplateFile(string fileName, string content)
    {
        // 使用 System.IO.Path 和 System.IO.Directory 确保没有命名冲突
        string tempDir = System.IO.Path.Combine(Application.dataPath, "../Temp/CustomScriptTemplates");

        if (!System.IO.Directory.Exists(tempDir))
        {
            System.IO.Directory.CreateDirectory(tempDir);
        }

        string filePath = System.IO.Path.Combine(tempDir, fileName);
        System.IO.File.WriteAllText(filePath, content);
        return filePath;
    }

    // =========================================================
    // 模板内容定义区
    // =========================================================
    private static string GetNormalTemplate()
    {
        return
@"using UnityEngine;

public class #SCRIPTNAME# : MonoBehaviour
{

    //脚本首次启动时，会执行的代码块方法（在awake方法之后）
    void Start()
    {
        
    }

    //每次运行该脚本时，会执行的代码块方法
    void Update()
    {
        
    }
}";
    }

    private static string GetSOTemplate()
    {
        return
    @"using UnityEngine;

    //资产配置的栏数据显示（Scriptable Objects//SCRIPTNAME//是可该目录部分）
    [CreateAssetMenu(fileName = ""#SCRIPTNAME#"", menuName = ""SmallWarThunder/生成/资源配置/#SCRIPTNAME#"")]
    public class #SCRIPTNAME# : ScriptableObject
    {
    
    }";
    }

    private static string GetNullTemplate()
    {
        return
@"using  UnityEngine;

public class #SCRIPTNAME# : MonoBehaviour
{


}";
    }
}