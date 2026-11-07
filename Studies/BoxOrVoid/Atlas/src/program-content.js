export const bootStages=[
  {name:'点下运行',call:'BoxOrVoid.unity',detail:'Unity 载入场景，调用入口对象上的 PuzzleApp。',input:'已保存的场景引用',output:'PuzzleApp / Camera / Menu',file:'PuzzleApp.cs',anchor:'[SerializeField] private StudyAssets assets;',page:'场景载入'},
  {name:'设置运行环境',call:'PuzzleApp.Awake',detail:'应用渲染管线，设置目标帧率。',input:'StudyAssets.RenderPipeline',output:'60 FPS / 指定渲染管线',file:'PuzzleApp.cs',anchor:'private void Awake()',page:'初始化'},
  {name:'连接资源与组件',call:'PuzzleApp.Start',detail:'建立资源索引，读取进度，创建棋盘、HUD 和音频组件。',input:'Catalog / JSON / Sprite / Audio',output:'标题界面',file:'PuzzleApp.cs',anchor:'private void Start()',page:'Title'},
  {name:'玩家选关',call:'StudyMenu → LoadLevel',detail:'菜单选择章节和关卡，传入关卡 id。',input:'例如 n_box_3',output:'StudyAssets.Definition(id)',file:'PuzzleApp.cs',anchor:'public void LoadLevel(string id)',page:'Title → Chapters → Levels'},
  {name:'建立初始局面',call:'EnterPuzzle',detail:'JSON 变成关卡定义；新会话生成初始局面，交给棋盘显示。',input:'PuzzleDefinition',output:'PuzzleSession.State',file:'PuzzleApp.cs',anchor:'private void EnterPuzzle',page:'Playing'},
  {name:'每帧接收输入',call:'PuzzleApp.Update',detail:'StudyInput 将按键转换成指令；撤销和重置先进入等待队列。',input:'键盘 / 当前页面 / 动画状态',output:'Command',file:'PuzzleApp.cs',anchor:'var action=input.Poll',page:'Playing'},
  {name:'计算并分发结果',call:'Act → Session.Apply',detail:'规则计算下一局面，会话保存历史；App 分发画面、HUD 和音效。',input:'Command + 当前 State',output:'ActionResult（Before / After）',file:'PuzzleApp.cs',anchor:'public ActionResult Act',page:'Playing'},
  {name:'动画与过关',call:'BoardView.Update / IsSolved',detail:'画面逐帧插值；全部目标满足后记录进度，播放完成效果并回到选关。',input:'Before / After / 时间',output:'下一帧，或返回选关',file:'PuzzleApp.cs',anchor:'if(session.State.IsSolved',page:'Playing → Completing → Levels'}
];

export const components=[
  {name:'StudyAssets',role:'资源与关卡索引',type:'ScriptableObject',group:'data',file:'StudyAssets.cs',anchor:'public void Initialize()',input:'Catalog、关卡 JSON、贴图、音频',output:'关卡定义 / 主题贴图 / 音频',owns:'资源字典；不保存正在玩的局面'},
  {name:'PuzzleDefinition',role:'静态棋盘',type:'普通 C# 对象',group:'data',file:'PuzzleDefinition.cs',anchor:'public static PuzzleDefinition FromData',input:'LevelData JSON',output:'墙、目标、初始动体',owns:'尺寸、墙色、目标条件、初始配置'},
  {name:'StudyInput',role:'按键转指令',type:'普通 C# 对象',group:'data',file:'StudyInput.cs',anchor:'public StudyAction? Poll',input:'键盘、时间、页面、动画状态',output:'一个 StudyAction，或无输入',owns:'按住的动作、下次连发时间'},
  {name:'PuzzleApp',role:'入口与流程协调',type:'MonoBehaviour',group:'logic',file:'PuzzleApp.cs',anchor:'public ActionResult Act',input:'菜单选择、输入指令',output:'调用会话 / 画面 / HUD / 音频',owns:'Page、当前关卡、会话与组件引用'},
  {name:'PuzzleSession',role:'当前局面与历史',type:'普通 C# 对象',group:'logic',file:'PuzzleSession.cs',anchor:'public sealed class PuzzleSession',input:'Command',output:'ActionResult',owns:'State、历史栈、重置检查点'},
  {name:'PuzzleRules',role:'判断与移动计划',type:'静态 C# 类',group:'logic',file:'PuzzleSession.cs',anchor:'private static bool PlanMove',input:'State + 方向',output:'新 State；成功 / 受阻',owns:'临时递归栈与移动顺序'},
  {name:'PuzzleState',role:'一份完整局面',type:'普通 C# 对象',group:'logic',file:'PuzzleState.cs',anchor:'public sealed class PuzzleState',input:'动体位置、层序、控制色',output:'TopAt / GoalFulfilled / IsSolved',owns:'ActiveTone、Pieces；引用静态 Definition'},
  {name:'BoardView',role:'局面投影成画面',type:'MonoBehaviour',group:'view',file:'BoardView.cs',anchor:'public void Present(ActionResult',input:'ActionResult + 主题资源',output:'Sprite / 层序 / 位置插值 / 墙边',owns:'渲染器、Before / After、动画计时'},
  {name:'PuzzleHud',role:'操作提示与进度',type:'MonoBehaviour',group:'view',file:'PuzzleHud.cs',anchor:'public void Refresh',input:'当前关卡 + State',output:'提示 / 目标数 / 控制色',owns:'HUD 视图'},
  {name:'PuzzleAudio',role:'动作与音乐反馈',type:'MonoBehaviour',group:'view',file:'PuzzleAudio.cs',anchor:'public void Initialize',input:'语义音效 key、章节、音量',output:'音效与音乐 AudioSource',owns:'两个播放通道'},
  {name:'HistoryActionQueue',role:'等待动画结束',type:'普通 C# 对象',group:'support',file:'HistoryActionQueue.cs',anchor:'public Command? Tick',input:'撤销 / 重置请求、IsAnimating',output:'连续两帧稳定后交出指令',owns:'一个待处理请求；新请求替换旧请求'},
  {name:'ProgressStore',role:'保存玩家进度',type:'普通 C# 对象',group:'support',file:'ProgressStore.cs',anchor:'public bool Save',input:'完成关卡 / 设置 / 最后选择',output:'persistentDataPath 下的存档',owns:'通关、章节与设置数据'},
  {name:'StudyMenu',role:'标题与选关页面',type:'MonoBehaviour',group:'support',file:'StudyMenu.cs',anchor:'public void Handle',input:'方向、确认、返回、鼠标',output:'PuzzleApp 的页面 / 选关调用',owns:'菜单选择与已制作的 UI 引用'}
];
