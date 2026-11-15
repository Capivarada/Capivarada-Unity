using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;


public class BuildAll
{
    
    [MenuItem("Build/1. All Platforms")]
    public static void BuildAllPlatforms()
    {
        BuildPCPlatforms();
        BuildMobilePlatforms();
    }
    
    [MenuItem("Build/2. PC Platforms")]
    public static void BuildPCPlatforms()
    {
        BuildWebGL();
        BuildLinux();
        BuildWindows();
    }
    
    [MenuItem("Build/3. Mobile Platforms")]
    public static void BuildMobilePlatforms()
    {
        BuildAndroid ();
    }
    
    [MenuItem("Build/4. Windows")]
    public static void BuildWindows()
    {
        string[] scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
        string projectName = PlayerSettings.productName;
        string bundleVersion = PlayerSettings.bundleVersion;
        
        // Windows

        BuildPlayerOptions windows = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = $"Builds/Windows/{projectName} (Windows V{bundleVersion})/{projectName}.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(windows);
        
        Debug.Log("Build Windows Concluída!");
    }
    
   [MenuItem("Build/5. Linux Dedicated Server (PlayFab)")]
public static void BuildLinux()
{
    string[] scenes =
        EditorBuildSettingsScene.GetActiveSceneList(
            EditorBuildSettings.scenes);

    string buildDirectory = Path.GetFullPath(
        Path.Combine("Builds", "Linux", "CapivaradaServer"));

    string executablePath = Path.Combine(
        buildDirectory, "CapivaradaServer.x86_64");

    string zipPath = Path.GetFullPath(
        Path.Combine("Builds", "Linux", "CapivaradaServer.zip"));

    Directory.CreateDirectory(Path.GetDirectoryName(buildDirectory));

    if (Directory.Exists(buildDirectory))
        Directory.Delete(buildDirectory, true);

    Directory.CreateDirectory(buildDirectory);

    BuildPlayerOptions options = new BuildPlayerOptions
    {
        scenes = scenes,
        locationPathName = executablePath,
        target = BuildTarget.StandaloneLinux64,
        subtarget = (int)StandaloneBuildSubtarget.Server,
        options = BuildOptions.None
    };

    BuildReport report = BuildPipeline.BuildPlayer(options);

    if (report.summary.result != BuildResult.Succeeded)
    {
        Debug.LogError(
            $"Falha na build Linux: {report.summary.result}");
        return;
    }

    if (!File.Exists(executablePath))
    {
        Debug.LogError("Executável Linux não encontrado.");
        return;
    }

    
    string startupScript = Path.Combine(buildDirectory, "start.sh");

    string script =
        "#!/bin/bash\n" +
        "set -e\n" +
        "\n" +
        "SOURCE_DIR=\"$(cd \"$(dirname \"$0\")\" && pwd)\"\n" +
        "RUNTIME_DIR=\"$(mktemp -d /tmp/capivarada-server.XXXXXX)\"\n" +
        "\n" +
        "echo \"[Capivarada] Preparando servidor...\"\n" +
        "cp -a \"$SOURCE_DIR/.\" \"$RUNTIME_DIR/\"\n" +
        "chmod +x \"$RUNTIME_DIR/CapivaradaServer.x86_64\"\n" +
        "\n" +
        "# Configuracao do PlayFab GSDK\n" +
        "if [ -z \"${GSDK_CONFIG_FILE:-}\" ]; then\n" +
        "    export GSDK_CONFIG_FILE=\"/mnt/Config/SH0/gsdkConfig.json\"\n" +
        "    echo \"[Capivarada] GSDK_CONFIG_FILE configurado pelo fallback.\"\n" +
        "fi\n" +
        "\n" +
        "if [ ! -f \"$GSDK_CONFIG_FILE\" ]; then\n" +
        "    echo \"[Capivarada] ERRO: Configuracao GSDK nao encontrada: $GSDK_CONFIG_FILE\" >&2\n" +
        "    exit 1\n" +
        "fi\n" +
        "\n" +
        "echo \"[Capivarada] Configuracao GSDK encontrada.\"\n" +
        "cd \"$RUNTIME_DIR\"\n" +
        "echo \"[Capivarada] Iniciando Unity Dedicated Server...\"\n" +
        "\n" +
        "exec ./CapivaradaServer.x86_64 \"$@\"\n";

    File.WriteAllText(
        startupScript,
        script,
        new System.Text.UTF8Encoding(false)
    );


    if (File.Exists(zipPath))
        File.Delete(zipPath);

    using (ZipArchive archive =
           ZipFile.Open(zipPath, ZipArchiveMode.Create))
    {
        foreach (string file in Directory.GetFiles(
                     buildDirectory,
                     "*",
                     SearchOption.AllDirectories))
        {
            string relativePath =
                Path.GetRelativePath(buildDirectory, file)
                    .Replace('\\', '/');

            ZipArchiveEntry entry = archive.CreateEntryFromFile(
                file,
                relativePath,
                System.IO.Compression.CompressionLevel.Optimal);

            bool executable =
                relativePath == "CapivaradaServer.x86_64";

            // Unix regular file + permissões 755 ou 644.
            entry.ExternalAttributes =
                (executable ? 0x81ED : 0x81A4) << 16;
        }
    }

    Debug.Log($"Build PlayFab concluída: {zipPath}");
}
    
    [MenuItem("Build/6. WebGL")]
    public static void BuildWebGL()
    {
        string[] scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
        string projectName = PlayerSettings.productName;
        string bundleVersion = PlayerSettings.bundleVersion;
        
        // WebGL

        BuildPlayerOptions web = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = $"Builds/WebGL/{projectName} (WebGL V{bundleVersion})",
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(web);
        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"Build WebGL concluída: {report.summary.outputPath}");
        else
            Debug.LogError($"Build WebGL falhou: {report.summary.result}. Consulte os erros acima; nenhum artefato válido foi gerado.");
    }
    
    [MenuItem("Build/7. Android")]
    public static void BuildAndroid()
    {
        string[] scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
        string projectName = PlayerSettings.productName;
        string bundleVersion = PlayerSettings.bundleVersion;
        
        // Android

        BuildPlayerOptions android = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = $"Builds/Android/{projectName} (V{bundleVersion}).apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildPipeline.BuildPlayer(android);
        
        Debug.Log("Build Android Concluída!");
    }
    
    [MenuItem("Build/8. Clear All")]
    public static void ClearBuilds()
    {
        string pathName = Path.Combine(Application.dataPath, "..", "Builds");
        pathName = Path.GetFullPath(pathName);

        if (Directory.Exists(pathName))
        {
            Directory.Delete(pathName, true);
        }

        Directory.CreateDirectory(pathName);

        Debug.Log($"Builds limpas: {pathName}");
    }
    
}
