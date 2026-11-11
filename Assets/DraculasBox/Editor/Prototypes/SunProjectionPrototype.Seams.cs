using System;
using System.Collections.Generic;
using System.IO;
using DraculasBox.Core;
using DraculasBox.Runtime;
using UnityEditor;
using UnityEngine;

namespace DraculasBox.Editor.Prototypes
{
    public static partial class SunProjectionPrototype
    {
        public static LevelData SeamRoom()=>new LevelData
        {
            id="shadow-seam-review",title="交叠投影",sunEnabled=true,sunFrom=SunSide.Left,
            rows=new[]{"#########","##....###","##.....##","##......#","##......#","###...###","#########"},
            player=new Cell(3,3),coffin=new Cell(4,5),pieces=new[]{new EntityData{id="box-a",cell=new Cell(2,2)},new EntityData{id="box-b",cell=new Cell(3,4)},new EntityData{id="lamp",kind=PieceKind.Lamp,cell=new Cell(3,5)}}
        };
        [Serializable] private sealed class SeamResult
        {public string tag;public float internalEdgeJump;public bool passed;public Color[] samples;}
        public static string ReviewSeams(string tag)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Editor busy.");
            string folder=Path.GetFullPath("DraculasBox/Reviews/ShadowSeams-20261111");Directory.CreateDirectory(folder);
            var assets=AssetDatabase.LoadAssetAtPath<GameAssets>(GameBuilder.AssetsPath);string theme=GameThemes.Current.Id;var pipeline=QualitySettings.renderPipeline;
            try
            {
                QualitySettings.renderPipeline=assets.pipeline;GameThemes.Apply("wine");
                using(var stage=new Stage(assets,SeamRoom()))
                {
                    var p=stage.Board.Position(new Cell(3,2));
                    var colors=stage.SamplePixels(new[]{p+new Vector3(.20f,-.47f),p+new Vector3(.20f,-.53f)});
                    float jump=Mathf.Abs(colors[0].grayscale-colors[1].grayscale);var result=new SeamResult{tag=tag,samples=colors,internalEdgeJump=jump,passed=jump<.12f};
                    stage.Capture(folder,tag,new List<string>());string json=JsonUtility.ToJson(result,true);File.WriteAllText(Path.Combine(folder,tag+".json"),json);
                    using(var actual=new Stage(assets,assets.Level("c1_07")))actual.Capture(folder,tag+"-c1_07",new List<string>());
                    return json;
                }
            }
            finally{GameThemes.Apply(theme);QualitySettings.renderPipeline=pipeline;}
        }
    }
}
